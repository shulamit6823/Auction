using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Auction.Tests.Unit;

using Auction.Core.Entities;
using Auction.Core.Exceptions;
using Auction.Core.Interfaces;
using Auction.Core.Models;
using Auction.Services;

// Unit tests: the repositories are fakes (Moq), no database. Only the business logic is tested.
public class AuctionServiceTests
{
    private const int SellerId = 1;

    private readonly Mock<IAuctionRepository> _auctionRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IBidRepository> _bidRepository = new();
    private readonly AuctionService _service;

    public AuctionServiceTests()
    {
        _userRepository.Setup(r => r.GetByIdAsync(SellerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = SellerId });

        _service = new AuctionService(_auctionRepository.Object, _userRepository.Object, _bidRepository.Object,
            NullLogger<AuctionService>.Instance);
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_ValidData_CreatesPendingAuction()
    {
        var auction = await _service.CreateAsync(SellerId, "  Bike  ", "Red bike", 100, 2, DurationUnit.Days);

        Assert.Equal(AuctionStatus.Pending, auction.Status);
        Assert.Equal("Bike", auction.Title);
        Assert.Equal(100, auction.CurrentPrice);
        Assert.Equal(48, auction.DurationHours);
        Assert.Null(auction.ApprovedAt);
        Assert.Null(auction.EndsAt);
        _auctionRepository.Verify(r => r.Add(auction), Times.Once);
        _auctionRepository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_StartingPriceZero_IsAllowed()
    {
        var auction = await _service.CreateAsync(SellerId, "Book", "", 0, 24, DurationUnit.Hours);

        Assert.Equal(0, auction.StartingPrice);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_EmptyTitle_Throws(string title)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.CreateAsync(SellerId, title, "", 10, 1, DurationUnit.Days));
    }

    [Fact]
    public async Task Create_NegativeStartingPrice_Throws()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.CreateAsync(SellerId, "Bike", "", -1, 1, DurationUnit.Days));
    }

    [Theory]
    [InlineData(0, DurationUnit.Hours)]   // must be positive
    [InlineData(31, DurationUnit.Days)]   // 744 hours - more than a month
    [InlineData(5, DurationUnit.Weeks)]   // 840 hours - more than a month
    public async Task Create_InvalidDuration_Throws(int amount, DurationUnit unit)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.CreateAsync(SellerId, "Bike", "", 10, amount, unit));
    }

    [Fact]
    public async Task Create_UnknownSeller_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateAsync(99, "Bike", "", 10, 1, DurationUnit.Days));
    }

    // ---------- Approve ----------

    [Fact]
    public async Task Approve_Pending_BecomesActiveAndTimeStartsNow()
    {
        var auction = SetupAuction(new Auction { Id = 1, Status = AuctionStatus.Pending, DurationHours = 24, Version = 0 });

        await _service.ApproveAsync(1);

        Assert.Equal(AuctionStatus.Active, auction.Status);
        Assert.NotNull(auction.ApprovedAt);
        Assert.Equal(auction.ApprovedAt!.Value.AddHours(24), auction.EndsAt);
        Assert.Equal(1, auction.Version);
    }

    [Theory]
    [InlineData(AuctionStatus.Active)]
    [InlineData(AuctionStatus.Closed)]
    public async Task Approve_NotPending_Throws(AuctionStatus status)
    {
        SetupAuction(new Auction { Id = 1, Status = status });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ApproveAsync(1));
    }

    // ---------- Reject ----------

    [Fact]
    public async Task Reject_Pending_BecomesRejectedWithReason()
    {
        var auction = SetupAuction(new Auction { Id = 1, Status = AuctionStatus.Pending, Version = 0 });

        await _service.RejectAsync(1, "  Missing details  ");

        Assert.Equal(AuctionStatus.Rejected, auction.Status);
        Assert.Equal("Missing details", auction.RejectionReason);
        Assert.Equal(1, auction.Version);
    }

    [Fact]
    public async Task Reject_WithoutReason_Throws()
    {
        SetupAuction(new Auction { Id = 1, Status = AuctionStatus.Pending });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RejectAsync(1, "  "));
    }

    [Fact]
    public async Task Reject_Active_Throws()
    {
        SetupAuction(new Auction { Id = 1, Status = AuctionStatus.Active });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RejectAsync(1, "reason"));
    }

    // ---------- Seller: edit / cancel ----------

    [Fact]
    public async Task Update_RejectedBySeller_BackToPendingWithNewDetails()
    {
        var auction = SetupAuction(new Auction
        {
            Id = 1, SellerId = SellerId, Status = AuctionStatus.Rejected, RejectionReason = "No photo", Version = 2
        });

        await _service.UpdateAsync(1, SellerId, "New title", "New text", 50, 3, DurationUnit.Days);

        Assert.Equal(AuctionStatus.Pending, auction.Status);
        Assert.Null(auction.RejectionReason);
        Assert.Equal("New title", auction.Title);
        Assert.Equal(50, auction.CurrentPrice);
        Assert.Equal(72, auction.DurationHours);
        Assert.Equal(3, auction.Version);
    }

    [Fact]
    public async Task Update_NotTheSeller_ThrowsForbidden()
    {
        SetupAuction(new Auction { Id = 1, SellerId = SellerId, Status = AuctionStatus.Pending });

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UpdateAsync(1, sellerId: 2, "Bike", "", 10, 1, DurationUnit.Days));
    }

    [Theory]
    [InlineData(AuctionStatus.Active)]
    [InlineData(AuctionStatus.Closed)]
    public async Task UpdateOrCancel_AfterApproval_Throws(AuctionStatus status)
    {
        SetupAuction(new Auction { Id = 1, SellerId = SellerId, Status = status });

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.UpdateAsync(1, SellerId, "Bike", "", 10, 1, DurationUnit.Days));
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CancelAsync(1, SellerId));
        _auctionRepository.Verify(r => r.Remove(It.IsAny<Auction>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_PendingBySeller_IsRemoved()
    {
        var auction = SetupAuction(new Auction { Id = 1, SellerId = SellerId, Status = AuctionStatus.Pending });

        await _service.CancelAsync(1, SellerId);

        _auctionRepository.Verify(r => r.Remove(auction), Times.Once);
    }

    // ---------- Delete (Admin restriction) ----------

    [Fact]
    public async Task Delete_ActiveAuction_Throws()
    {
        SetupAuction(new Auction { Id = 1, Status = AuctionStatus.Active });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DeleteAsync(1));
        _auctionRepository.Verify(r => r.Remove(It.IsAny<Auction>()), Times.Never);
    }

    [Fact]
    public async Task Delete_PendingAuction_IsRemoved()
    {
        var auction = SetupAuction(new Auction { Id = 1, Status = AuctionStatus.Pending });

        await _service.DeleteAsync(1);

        _auctionRepository.Verify(r => r.Remove(auction), Times.Once);
    }

    // ---------- Closing / Winner ----------

    [Fact]
    public async Task CloseEnded_WithBids_HighestBidderWins()
    {
        var auction = new Auction { Id = 1, Status = AuctionStatus.Active, Version = 3 };
        _auctionRepository.Setup(r => r.GetActiveEndedBeforeAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { auction });
        _bidRepository.Setup(r => r.GetHighestBidAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Bid { AuctionId = 1, BidderId = 7, Amount = 500 });

        var closed = await _service.CloseEndedAuctionsAsync();

        Assert.Equal(1, closed);
        Assert.Equal(AuctionStatus.Closed, auction.Status);
        Assert.Equal(7, auction.WinnerId);
        Assert.Equal(4, auction.Version);
    }

    [Fact]
    public async Task CloseEnded_NoBids_NoWinner()
    {
        var auction = new Auction { Id = 1, Status = AuctionStatus.Active };
        _auctionRepository.Setup(r => r.GetActiveEndedBeforeAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { auction });

        await _service.CloseEndedAuctionsAsync();

        Assert.Equal(AuctionStatus.Closed, auction.Status);
        Assert.Null(auction.WinnerId);
    }

    // ---------- Visibility ----------

    [Fact]
    public async Task GetActive_EndedButNotClosedYet_IsNotReturned()
    {
        var open = new Auction { Id = 1, Status = AuctionStatus.Active, EndsAt = DateTime.UtcNow.AddHours(1) };
        var ended = new Auction { Id = 2, Status = AuctionStatus.Active, EndsAt = DateTime.UtcNow.AddMinutes(-1) };
        _auctionRepository.Setup(r => r.GetByStatusAsync(AuctionStatus.Active, It.IsAny<string?>(), It.IsAny<AuctionSort>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { open, ended });

        var result = await _service.GetActiveAsync();

        Assert.Equal(new[] { open }, result);
    }

    [Theory]
    [InlineData(SellerId, false)] // the seller
    [InlineData(50, true)]        // an Admin
    public async Task GetVisibleById_Pending_SellerAndAdminCanSee(int viewerId, bool isAdmin)
    {
        SetupAuction(new Auction { Id = 1, Status = AuctionStatus.Pending, SellerId = SellerId });

        var auction = await _service.GetVisibleByIdAsync(1, viewerId, isAdmin);

        Assert.Equal(1, auction.Id);
    }

    [Theory]
    [InlineData(AuctionStatus.Pending)]
    [InlineData(AuctionStatus.Rejected)]
    public async Task GetVisibleById_NotPublishedOfSomeoneElse_NotFound(AuctionStatus status)
    {
        SetupAuction(new Auction { Id = 1, Status = status, SellerId = SellerId });

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetVisibleByIdAsync(1, viewerId: 2, viewerIsAdmin: false));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetVisibleByIdAsync(1, viewerId: null, viewerIsAdmin: false));
    }

    private Auction SetupAuction(Auction auction)
    {
        _auctionRepository.Setup(r => r.GetByIdAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(auction);
        return auction;
    }
}
