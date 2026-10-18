using Microsoft.EntityFrameworkCore;

namespace Auction.Data;

using Auction.Core.Entities;
using Auction.Core.Interfaces;

// Demo data for development: an Admin, a few users, auctions in every status, bids and a favorite.
// Runs only when the DB has no users, so it never touches real data and never runs twice.
// Times are relative to "now", so the Active auctions are really open after every fresh seed.
public static class DataSeeder
{
    public static async Task SeedAsync(AuctionDbContext context, IPasswordHasher passwordHasher, string password)
    {
        if (await context.Users.AnyAsync())
            return;

        var hash = passwordHasher.Hash(password);
        var admin = new User { Username = "admin", Email = "admin@example.com", PasswordHash = hash, Role = UserRole.Admin };
        var dana = new User { Username = "dana", Email = "dana@example.com", PasswordHash = hash };
        var yossi = new User { Username = "yossi", Email = "yossi@example.com", PasswordHash = hash };
        var noa = new User { Username = "noa", Email = "noa@example.com", PasswordHash = hash };

        var now = DateTime.UtcNow;

        var camera = Active(dana, "מצלמת וינטג'", "מצלמת פילם משנות ה-80, עובדת מצוין.", 100, 72, now.AddHours(-24));
        var bike = Active(yossi, "אופני כביש", "שלדת אלומיניום, מידה M.", 0, 24, now.AddHours(-2));
        var boardGame = Active(noa, "משחק קופסה", "שלם, כולל כל החלקים.", 10, 1, now.AddMinutes(-40));
        var guitar = new Auction
        {
            Title = "גיטרה אקוסטית", Description = "ממתינה לאישור מנהל.",
            StartingPrice = 300, CurrentPrice = 300, Status = AuctionStatus.Pending,
            CreatedAt = now.AddHours(-1), DurationHours = 48, Seller = noa
        };
        var lamp = Active(dana, "מנורת שולחן", "המכירה נסגרה - הזוכה: noa.", 20, 24, now.AddDays(-2));

        var bids = new List<Bid>();
        AddBid(bids, camera, yossi, 150, now.AddHours(-20));
        AddBid(bids, camera, noa, 180, now.AddHours(-10));
        AddBid(bids, boardGame, dana, 15, now.AddMinutes(-30));
        AddBid(bids, lamp, yossi, 25, now.AddDays(-2).AddHours(3));
        AddBid(bids, lamp, noa, 30, now.AddDays(-2).AddHours(5));

        lamp.Status = AuctionStatus.Closed;
        lamp.Winner = noa;
        lamp.Version++;

        context.Users.AddRange(admin, dana, yossi, noa);
        context.Auctions.AddRange(camera, bike, boardGame, guitar, lamp);
        context.Bids.AddRange(bids);
        context.Favorites.Add(new Favorite { User = yossi, Auction = camera });

        await context.SaveChangesAsync();
    }

    // An approved auction, as if the Admin approved it at approvedAt (Version 1 = after the approval).
    private static Auction Active(User seller, string title, string description, decimal startingPrice, int durationHours, DateTime approvedAt)
    {
        return new Auction
        {
            Title = title, Description = description,
            StartingPrice = startingPrice, CurrentPrice = startingPrice, Status = AuctionStatus.Active,
            CreatedAt = approvedAt.AddHours(-1), DurationHours = durationHours,
            ApprovedAt = approvedAt, EndsAt = approvedAt.AddHours(durationHours),
            Seller = seller, Version = 1
        };
    }

    // Same effect on the auction as a real bid: new price and a new Version.
    private static void AddBid(List<Bid> bids, Auction auction, User bidder, decimal amount, DateTime createdAt)
    {
        bids.Add(new Bid { Auction = auction, Bidder = bidder, Amount = amount, CreatedAt = createdAt });
        auction.CurrentPrice = amount;
        auction.Version++;
    }
}
