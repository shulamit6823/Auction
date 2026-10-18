using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Auction.Tests.Db;

using Auction.Core.Entities;
using Auction.Core.Exceptions;
using Auction.Data;
using Auction.Data.Repositories;
using Auction.Services;

// M7-M10: the concurrency scenarios on the real PostgreSQL (real transactions and row locks).
[Collection(PostgresCollection.Name)]
public class PostgresConcurrencyTests
{
    private const decimal StartPrice = 100;
    private const int StartVersion = 1;

    // M7 + M10
    [Fact]
    public async Task TwoDbContexts_SameVersion_FirstSucceeds_SecondConflict_LoserBidNotSaved()
    {
        var (auctionId, bidderIds) = await SeedAsync(bidders: 2);

        using var contextA = new AuctionDbContext(PostgresTestDatabase.Options);
        using var contextB = new AuctionDbContext(PostgresTestDatabase.Options);
        // Both users read the auction (price 100, version 1) before anyone saves.
        await contextA.Auctions.FindAsync(auctionId);
        await contextB.Auctions.FindAsync(auctionId);

        await CreateBidService(contextA).PlaceBidAsync(auctionId, bidderIds[0], 150, StartVersion);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => CreateBidService(contextB).PlaceBidAsync(auctionId, bidderIds[1], 120, StartVersion));

        await AssertFinalStateAsync(auctionId, expectedBidderId: bidderIds[0], expectedPrice: 150);
    }

    // M8 + M9 + M10
    [Fact]
    public async Task TenSimultaneousBids_OnlyOneSucceeds()
    {
        const int bidders = 10;
        var (auctionId, bidderIds) = await SeedAsync(bidders);

        // Every bidder has its own DbContext (= its own request) and has already read version 1.
        var contexts = Enumerable.Range(0, bidders).Select(_ => new AuctionDbContext(PostgresTestDatabase.Options)).ToList();
        foreach (var context in contexts)
            await context.Auctions.FindAsync(auctionId);

        // All bids are sent at the same moment, on different threads.
        var tasks = Enumerable.Range(0, bidders).Select(i => Task.Run(async () =>
        {
            try
            {
                await CreateBidService(contexts[i]).PlaceBidAsync(auctionId, bidderIds[i], 200 + i, StartVersion);
                return (Index: i, Succeeded: true);
            }
            catch (ConcurrencyConflictException)
            {
                return (Index: i, Succeeded: false);
            }
        }));
        var results = await Task.WhenAll(tasks);
        contexts.ForEach(c => c.Dispose());

        var winner = Assert.Single(results, r => r.Succeeded); // M9: exactly one
        Assert.Equal(bidders - 1, results.Count(r => !r.Succeeded));

        await AssertFinalStateAsync(auctionId, expectedBidderId: bidderIds[winner.Index], expectedPrice: 200 + winner.Index);
    }

    // M10: price, version and Bids table all agree - only the winning bid exists.
    private static async Task AssertFinalStateAsync(int auctionId, int expectedBidderId, decimal expectedPrice)
    {
        using var check = new AuctionDbContext(PostgresTestDatabase.Options);
        var auction = await check.Auctions.SingleAsync(a => a.Id == auctionId);
        var bid = Assert.Single(await check.Bids.Where(b => b.AuctionId == auctionId).ToListAsync());

        Assert.Equal(expectedPrice, auction.CurrentPrice);
        Assert.Equal(StartVersion + 1, auction.Version);
        Assert.Equal(expectedBidderId, bid.BidderId);
        Assert.Equal(expectedPrice, bid.Amount);
    }

    private static async Task<(int AuctionId, int[] BidderIds)> SeedAsync(int bidders)
    {
        using var context = new AuctionDbContext(PostgresTestDatabase.Options);
        // Username and Email are unique in the DB - every test user gets its own.
        User NewUser(string name) => new() { Username = name, Email = $"{name}@test.com" };
        var seller = NewUser("seller-" + Guid.NewGuid());
        var users = Enumerable.Range(0, bidders).Select(i => NewUser($"bidder{i}-{Guid.NewGuid()}")).ToList();
        var auction = new Auction
        {
            Title = "Concurrency test",
            Seller = seller,
            Status = AuctionStatus.Active,
            StartingPrice = StartPrice,
            CurrentPrice = StartPrice,
            CreatedAt = DateTime.UtcNow,
            DurationHours = 1,
            ApprovedAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(1),
            Version = StartVersion
        };
        context.Users.AddRange(users);
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        return (auction.Id, users.Select(u => u.Id).ToArray());
    }

    private static BidService CreateBidService(AuctionDbContext context)
    {
        return new BidService(new BidRepository(context), new AuctionRepository(context), new UserRepository(context),
            NullLogger<BidService>.Instance);
    }
}
