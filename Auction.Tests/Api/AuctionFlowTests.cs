using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auction.Tests.Api;

using Auction.Core.DTOs;
using Auction.Core.Entities;
using Auction.Core.Interfaces;
using Auction.Data;
using Auction.Tests.Db;

// Stage L (the whole business flow) + N6 (the logs), through the real API on PostgreSQL.
// One scenario from start to end - every step depends on the one before it.
[Collection(PostgresCollection.Name)]
public class AuctionFlowTests : IClassFixture<PostgresApiFactory>
{
    private const string Password = "Test123!";

    // The API sends enums as strings ("Pending").
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly PostgresApiFactory _factory;

    public AuctionFlowTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullAuctionFlow()
    {
        // The test DB is rebuilt every run, so IDs repeat - check only the log lines this run writes.
        var logLinesBefore = ReadTodaysLog().Count;
        var (sellerId, bidderAId, bidderBId) = await SeedUsersAsync();
        var guest = _factory.CreateClient();
        var admin = await LoginAsync("admin");
        var seller = await LoginAsync("seller");
        var bidderA = await LoginAsync("bidderA");
        var bidderB = await LoginAsync("bidderB");

        // L1 - Create Auction
        var response = await seller.PostAsJsonAsync("api/auctions",
            new { Title = "Bike", Description = "Red", StartingPrice = 100, DurationAmount = 1, DurationUnit = "Hours" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auction = await ReadAsync<AuctionResponse>(response);
        var id = auction.Id;
        Assert.Equal(sellerId, auction.SellerId);
        Assert.Equal(100, auction.CurrentPrice);

        // L2 - Pending: not published yet
        Assert.Equal(AuctionStatus.Pending, auction.Status);
        Assert.Null(auction.EndsAt);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync($"api/auctions/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bidderA.GetAsync($"api/auctions/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await seller.GetAsync($"api/auctions/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"api/auctions/{id}")).StatusCode);
        Assert.DoesNotContain(await GetPublicListAsync(guest), a => a.Id == id);
        Assert.Equal(HttpStatusCode.BadRequest, (await BidAsync(bidderA, id, 150, auction.Version)).StatusCode);

        // L3 - Admin approval (a regular user is rejected)
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.PutAsync($"api/auctions/{id}/approve", null)).StatusCode);
        response = await admin.PutAsync($"api/auctions/{id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        auction = await ReadAsync<AuctionResponse>(response);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsync($"api/auctions/{id}/approve", null)).StatusCode);

        // L4 + L5 + L6 - Active, and the time counts from the approval
        Assert.Equal(AuctionStatus.Active, auction.Status);
        Assert.Equal(auction.ApprovedAt!.Value.AddHours(1), auction.EndsAt!.Value, TimeSpan.FromSeconds(1));
        Assert.Contains(await GetPublicListAsync(guest), a => a.Id == id);

        // L7 - Bid
        Assert.Equal(HttpStatusCode.Created, (await BidAsync(bidderA, id, 150, auction.Version)).StatusCode);
        auction = await GetAuctionAsync(guest, id);
        Assert.Equal(150, auction.CurrentPrice);
        Assert.Equal(HttpStatusCode.BadRequest, (await BidAsync(bidderB, id, 150, auction.Version)).StatusCode); // equal
        Assert.Equal(HttpStatusCode.BadRequest, (await BidAsync(seller, id, 500, auction.Version)).StatusCode);  // own auction

        // M - a bid by an old version -> 409; after reloading it succeeds
        Assert.Equal(HttpStatusCode.Conflict, (await BidAsync(bidderB, id, 200, auction.Version - 1)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await BidAsync(bidderB, id, 200, auction.Version)).StatusCode);

        // L8 - Repeated bids: every bid is saved separately
        auction = await GetAuctionAsync(guest, id);
        Assert.Equal(HttpStatusCode.Created, (await BidAsync(bidderA, id, 300, auction.Version)).StatusCode);
        var bids = await (await guest.GetAsync($"api/auctions/{id}/bids")).Content.ReadFromJsonAsync<List<BidResponse>>(Json);
        Assert.Equal(new decimal[] { 300, 200, 150 }, bids!.Select(b => b.Amount));

        // L12 - Admin cannot delete an active auction
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync($"api/auctions/{id}")).StatusCode);

        // L13 - Favorites
        Assert.True((await bidderB.PostAsync($"api/favorites/{id}", null)).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await bidderB.PostAsync($"api/favorites/{id}", null)).StatusCode); // twice
        var favorites = await (await bidderB.GetAsync("api/favorites")).Content.ReadFromJsonAsync<List<AuctionResponse>>(Json);
        Assert.Contains(favorites!, a => a.Id == id);
        Assert.True((await bidderB.DeleteAsync($"api/favorites/{id}")).IsSuccessStatusCode);
        var pending = await ReadAsync<AuctionResponse>(await seller.PostAsJsonAsync("api/auctions",
            new { Title = "Lamp", Description = "", StartingPrice = 0, DurationAmount = 1, DurationUnit = "Days" }));
        Assert.Equal(HttpStatusCode.NotFound, (await bidderB.PostAsync($"api/favorites/{pending.Id}", null)).StatusCode);

        // L11 - The time is over (moved to the past in the DB), the closing service has not run yet
        await SetEndsAtAsync(id, DateTime.UtcNow.AddMinutes(-1));
        auction = await GetAuctionAsync(guest, id);
        Assert.Equal(AuctionStatus.Active, auction.Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await BidAsync(bidderB, id, 1000, auction.Version)).StatusCode);
        Assert.DoesNotContain(await GetPublicListAsync(guest), a => a.Id == id);

        // L9 + L10 - Closing and Winner (what the background service does every minute)
        using (var scope = _factory.Services.CreateScope())
        {
            var closed = await scope.ServiceProvider.GetRequiredService<IAuctionService>().CloseEndedAuctionsAsync();
            Assert.True(closed >= 1);
        }
        auction = await GetAuctionAsync(guest, id);
        Assert.Equal(AuctionStatus.Closed, auction.Status);
        Assert.Equal(bidderAId, auction.WinnerId);
        Assert.Equal(300, auction.CurrentPrice);
        Assert.Equal(HttpStatusCode.BadRequest, (await BidAsync(bidderB, id, 1000, auction.Version)).StatusCode);

        // N6 - The business events and the concurrency conflict are in the log file
        var log = ReadTodaysLog().Skip(logLinesBefore).ToList();
        var createdLine = Assert.Single(log, l => l.Contains($"Auction {id} created by user {sellerId}"));
        Assert.True(Guid.TryParse(createdLine.Split(" | ")[2], out _), "The line should carry the RequestId.");
        Assert.Contains(log, l => l.Contains($"Auction {id} approved"));
        Assert.Contains(log, l => l.Contains($"placed by user {bidderAId} on auction {id}"));
        Assert.Contains(log, l => l.Contains("WARN") && l.Contains($"Concurrency: bid of 200 by user {bidderBId} on auction {id}"));
        Assert.Contains(log, l => l.Contains($"Auction {id} closed. Winner: {bidderAId}, final price 300"));
        Assert.DoesNotContain(log, l => l.Contains(Password));
    }

    private async Task<(int SellerId, int BidderAId, int BidderBId)> SeedUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        var hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Hash(Password);

        User NewUser(string name, UserRole role = UserRole.User) =>
            new() { Username = name, Email = $"{name}@test.com", PasswordHash = hash, Role = role };

        var users = new[] { NewUser("admin", UserRole.Admin), NewUser("seller"), NewUser("bidderA"), NewUser("bidderB") };
        context.Users.AddRange(users);
        await context.SaveChangesAsync();
        return (users[1].Id, users[2].Id, users[3].Id);
    }

    private async Task<HttpClient> LoginAsync(string username)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/login", new { Username = username, Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var login = await ReadAsync<LoginResponse>(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        return client;
    }

    private async Task SetEndsAtAsync(int auctionId, DateTime endsAt)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        var auction = await context.Auctions.SingleAsync(a => a.Id == auctionId);
        auction.EndsAt = endsAt;
        await context.SaveChangesAsync();
    }

    private static Task<HttpResponseMessage> BidAsync(HttpClient client, int auctionId, decimal amount, int version)
    {
        return client.PostAsJsonAsync($"api/auctions/{auctionId}/bids", new { Amount = amount, Version = version });
    }

    private static async Task<AuctionResponse> GetAuctionAsync(HttpClient client, int auctionId)
    {
        return await ReadAsync<AuctionResponse>(await client.GetAsync($"api/auctions/{auctionId}"));
    }

    private static async Task<List<AuctionResponse>> GetPublicListAsync(HttpClient client)
    {
        return await ReadAsync<List<AuctionResponse>>(await client.GetAsync("api/auctions"));
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    // NLog writes to <bin>/logs/auction-<local date>.log (see nlog.config).
    private static List<string> ReadTodaysLog()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "logs", $"auction-{DateTime.Now:yyyy-MM-dd}.log");
        if (!File.Exists(path))
            return new List<string>();

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); // NLog keeps it open
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
