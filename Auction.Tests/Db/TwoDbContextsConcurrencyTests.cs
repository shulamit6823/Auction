using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Auction.Tests.Db;

using Auction.Core.Entities;
using Auction.Core.Exceptions;
using Auction.Data;
using Auction.Data.Repositories;
using Auction.Services;

// Two separate DbContexts = two users / two requests working on the same auction.
// Uses EF InMemory, which also checks the Version concurrency token.
// The same scenario will be run against PostgreSQL later (M7-M10).
public class TwoDbContextsConcurrencyTests
{
    private readonly DbContextOptions<AuctionDbContext> _options = new DbContextOptionsBuilder<AuctionDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()) // a new empty DB for every test
        .Options;

    public TwoDbContextsConcurrencyTests()
    {
        using var context = new AuctionDbContext(_options);
        context.Users.AddRange(
            new User { Id = 1, Username = "seller" },
            new User { Id = 2, Username = "userA" },
            new User { Id = 3, Username = "userB" });
        context.Auctions.Add(new Auction
        {
            Id = 1,
            Title = "Bike",
            SellerId = 1,
            Status = AuctionStatus.Active,
            StartingPrice = 100,
            CurrentPrice = 100,
            EndsAt = DateTime.UtcNow.AddHours(1),
            Version = 1
        });
        context.SaveChanges();
    }

    [Fact]
    public async Task TwoBidsOnSameVersion_FirstSucceeds_SecondGetsConflict()
    {
        using var contextA = new AuctionDbContext(_options);
        using var contextB = new AuctionDbContext(_options);
        var serviceA = CreateBidService(contextA);
        var serviceB = CreateBidService(contextB);

        // User B has already read the auction (price 100, version 1) - before A saves.
        await contextB.Auctions.FindAsync(1);

        await serviceA.PlaceBidAsync(1, bidderId: 2, amount: 150, version: 1);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => serviceB.PlaceBidAsync(1, bidderId: 3, amount: 120, version: 1));

        // The auction keeps A's bid only.
        using var check = new AuctionDbContext(_options);
        var auction = await check.Auctions.SingleAsync(a => a.Id == 1);
        Assert.Equal(150, auction.CurrentPrice);
        Assert.Equal(2, auction.Version);
        // Not checked here: that B's Bid row was not saved. InMemory has no transactions, so it keeps
        // the row even though the save failed. PostgreSQL rolls back the whole save - checked in M10.
    }

    [Fact]
    public async Task SecondUserReloads_AndBidsAgain_Succeeds()
    {
        using var contextA = new AuctionDbContext(_options);
        await CreateBidService(contextA).PlaceBidAsync(1, bidderId: 2, amount: 150, version: 1);

        // B reloads (a new request = a new DbContext) and sees price 150, version 2.
        using var contextB = new AuctionDbContext(_options);
        var reloaded = await contextB.Auctions.SingleAsync(a => a.Id == 1);
        await CreateBidService(contextB).PlaceBidAsync(1, bidderId: 3, amount: 200, version: reloaded.Version);

        using var check = new AuctionDbContext(_options);
        var auction = await check.Auctions.SingleAsync(a => a.Id == 1);
        Assert.Equal(200, auction.CurrentPrice);
        Assert.Equal(3, auction.Version);
        Assert.Equal(2, await check.Bids.CountAsync());
    }

    private static BidService CreateBidService(AuctionDbContext context)
    {
        return new BidService(new BidRepository(context), new AuctionRepository(context), new UserRepository(context),
            NullLogger<BidService>.Instance);
    }
}
