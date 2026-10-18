using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Auction.Tests.Unit;

using Auction.Core.Entities;
using Auction.Core.Exceptions;
using Auction.Core.Interfaces;
using Auction.Services;

public class BidServiceTests
{
    private const int SellerId = 1;
    private const int BidderId = 2;

    private readonly Mock<IBidRepository> _bidRepository = new();
    private readonly Mock<IAuctionRepository> _auctionRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly BidService _service;

    // An open auction: Active, ends in an hour, current price 100, version 5.
    private readonly Auction _auction = new()
    {
        Id = 1,
        SellerId = SellerId,
        Status = AuctionStatus.Active,
        CurrentPrice = 100,
        EndsAt = DateTime.UtcNow.AddHours(1),
        Version = 5
    };

    public BidServiceTests()
    {
        _auctionRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(_auction);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new User { Id = id });

        _service = new BidService(_bidRepository.Object, _auctionRepository.Object, _userRepository.Object,
            NullLogger<BidService>.Instance);
    }

    [Fact]
    public async Task PlaceBid_HigherThanCurrentPrice_Succeeds()
    {
        var bid = await _service.PlaceBidAsync(1, BidderId, 150, version: 5);

        Assert.Equal(150, bid.Amount);
        Assert.Equal(150, _auction.CurrentPrice);
        Assert.Equal(6, _auction.Version);
        _bidRepository.Verify(r => r.Add(bid), Times.Once);
        _bidRepository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyBids_OneRowPerAuction_WithMyHighestBidAndLeading()
    {
        var leading = new Auction { Id = 1, Status = AuctionStatus.Active, CurrentPrice = 150 };
        var outbid = new Auction { Id = 2, Status = AuctionStatus.Active, CurrentPrice = 300 };
        _bidRepository.Setup(r => r.GetByBidderAsync(BidderId, It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            new Bid { AuctionId = 1, Auction = leading, Amount = 120 },
            new Bid { AuctionId = 1, Auction = leading, Amount = 150 },
            new Bid { AuctionId = 2, Auction = outbid, Amount = 200 }
        });

        var result = (await _service.GetMyBidsAsync(BidderId)).ToList();

        Assert.Equal(2, result.Count);
        var first = result.Single(s => s.Auction.Id == 1);
        Assert.Equal(150, first.MyHighestBid);
        Assert.True(first.IsLeading);
        Assert.False(result.Single(s => s.Auction.Id == 2).IsLeading);
    }

    [Theory]
    [InlineData(100)] // equal to the current price
    [InlineData(50)]  // lower
    public async Task PlaceBid_NotHigherThanCurrentPrice_Throws(decimal amount)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.PlaceBidAsync(1, BidderId, amount, version: 5));
        Assert.Equal(100, _auction.CurrentPrice);
    }

    [Fact]
    public async Task PlaceBid_RepeatedBidsBySameUser_AllSaved()
    {
        await _service.PlaceBidAsync(1, BidderId, 150, version: 5);
        await _service.PlaceBidAsync(1, BidderId, 200, version: 6);

        Assert.Equal(200, _auction.CurrentPrice);
        _bidRepository.Verify(r => r.Add(It.IsAny<Bid>()), Times.Exactly(2));
    }

    [Theory]
    [InlineData(AuctionStatus.Pending)]
    [InlineData(AuctionStatus.Closed)]
    public async Task PlaceBid_AuctionNotActive_Throws(AuctionStatus status)
    {
        _auction.Status = status;

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.PlaceBidAsync(1, BidderId, 150, version: 5));
    }

    [Fact]
    public async Task PlaceBid_AfterEndsAt_ThrowsEvenIfNotClosedYet()
    {
        _auction.EndsAt = DateTime.UtcNow.AddSeconds(-1); // Status is still Active

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.PlaceBidAsync(1, BidderId, 150, version: 5));
    }

    [Fact]
    public async Task PlaceBid_SellerOnOwnAuction_Throws()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.PlaceBidAsync(1, SellerId, 150, version: 5));
    }

    [Fact]
    public async Task PlaceBid_UnknownAuction_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.PlaceBidAsync(99, BidderId, 150, version: 5));
    }

    // ---------- Concurrency ----------

    [Fact]
    public async Task PlaceBid_StaleVersion_ThrowsConflictAndSavesNothing()
    {
        // The client loaded the auction at version 4 - someone has changed it since.
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => _service.PlaceBidAsync(1, BidderId, 150, version: 4));

        Assert.Equal(100, _auction.CurrentPrice);
        _bidRepository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PlaceBid_ConflictOnSave_IsPassedToCaller()
    {
        // Another bid was saved between our read and our save (the DB Version check failed).
        _bidRepository.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict"));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => _service.PlaceBidAsync(1, BidderId, 150, version: 5));
    }
}
