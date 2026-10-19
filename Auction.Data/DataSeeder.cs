using Microsoft.EntityFrameworkCore;

namespace Auction.Data;

using Auction.Core.Entities;
using Auction.Core.Interfaces;

// Demo data, so the site looks like a real, active marketplace:
// 1. Basic set - an Admin, 3 users and 5 auctions. Only when the DB has no users at all.
// 2. Marketplace set - more users, ~20 auctions in every status, many bids and favorites.
//    Only once: skipped if its first auction already exists. Existing data is never changed or deleted.
// Times are relative to "now", so the Active auctions are really open after a fresh seed.
public static class DataSeeder
{
    public static async Task SeedAsync(AuctionDbContext context, IPasswordHasher passwordHasher, string password)
    {
        var hash = passwordHasher.Hash(password);
        await SeedBasicAsync(context, hash);
        await SeedMarketplaceAsync(context, hash);
    }

    private static async Task SeedBasicAsync(AuctionDbContext context, string hash)
    {
        if (await context.Users.AnyAsync())
            return;

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

    // ---------- Marketplace set ----------

    private static readonly string[] MarketplaceUsernames =
    {
        "dana", "yossi", "noa", "michal", "avi", "tamar", "eitan", "shira",
        "moshe", "rivka", "daniel", "yael", "omer", "hila", "ariel", "lior"
    };

    // StartedHoursAgo = when the Admin approved it. Bigger than DurationHours = already Closed.
    private record DemoAuction(string Title, string Description, decimal StartingPrice, int DurationHours,
        double StartedHoursAgo, int BidCount);

    private static readonly DemoAuction[] OpenAndClosed =
    {
        new("אייפון 13 128GB", "במצב מצוין, סוללה 89%. כולל קופסה מקורית ומטען.", 1500, 72, 30, 9),
        new("מחשב נייד Lenovo ThinkPad", "i7, זיכרון 16GB, SSD 512GB. מתאים ללימודים ולעבודה.", 1800, 120, 20, 6),
        new("ספה תלת-מושבית אפורה", "ריפוד בד, נקייה ושמורה. איסוף עצמי מירושלים.", 400, 48, 40, 5),
        new("PlayStation 5 + שני בקרים", "כולל משחק FIFA. עובד מושלם, בלי שריטות.", 1600, 24, 23.3, 11),
        new("שעון יד Casio וינטג'", "משנות ה-90, עובד, רצועה מקורית.", 80, 168, 50, 4),
        new("אוזניות Sony WH-1000XM4", "סינון רעשים אקטיבי, כמעט חדשות, עם נרתיק.", 600, 96, 10, 7),
        new("אופני הרים Trek", "מידה L, בלמי דיסק, עברו טיפול לפני חודש.", 1200, 72, 60, 8),
        new("סט ספרי הארי פוטר", "7 ספרים בעברית, כריכה רכה, מצב טוב.", 120, 120, 5, 3),
        new("מכונת קפה Delonghi", "עם מקציף חלב. ניקוי אבנית נעשה לאחרונה.", 350, 48, 47.5, 6),
        new("גיטרה חשמלית Fender Squier", "כולל מגבר קטן, כבל ורצועה.", 900, 240, 30, 5),
        new("שולחן אוכל מעץ מלא + 6 כיסאות", "עץ אלון, מתאים למשפחה גדולה.", 1100, 168, 100, 2),
        new("רחפן DJI Mini 2", "3 סוללות ותיק נשיאה. פחות מ-10 טיסות.", 1300, 72, 2, 1),
        new("ציור שמן מקורי - נוף ירושלים", "60x80 ס\"מ, ממוסגר וחתום על ידי האמן.", 500, 336, 24, 0),
        new("קורקינט חשמלי Xiaomi", "טווח של 25 ק\"מ, עם מטען מקורי.", 700, 48, 12, 4),
        new("מצלמת Canon EOS 250D", "עם עדשת 18-55 וכרטיס זיכרון 64GB.", 1400, 120, 70, 6),
        // Closed
        new("טלוויזיה Samsung 55 אינץ'", "4K חכמה, כולל שלט ומתקן קיר.", 1000, 72, 100, 10),
        new("עגלת תינוק Bugaboo", "מלאה עם כל האביזרים, שמורה מאוד.", 800, 48, 80, 5),
        new("מקלדת מכנית RGB", "מתגים אדומים, תאורה צבעונית.", 150, 24, 50, 4),
        new("שמלת ערב - מידה 38", "נלבשה פעם אחת בלבד.", 200, 48, 70, 0)
    };

    private static readonly (string Title, string Description, decimal StartingPrice, int DurationHours)[] Pending =
    {
        ("אוסף בולים ישן", "בולי ישראל משנות ה-50 וה-60, באלבום מסודר.", 300, 168),
        ("מקרר Bosch", "2 דלתות, No Frost, עובד מצוין.", 900, 72)
    };

    private static readonly (string Title, string Description, decimal StartingPrice, int DurationHours, string Reason)[] Rejected =
    {
        ("כרטיס להופעה", "כרטיס יחיד להופעה בחודש הבא.", 250, 48, "אסור למכור כרטיסים להופעות במערכת."),
        ("מוצר", "", 10, 24, "חסר תיאור של המוצר. ערכו את המכירה ושלחו שוב.")
    };

    private static async Task SeedMarketplaceAsync(AuctionDbContext context, string hash)
    {
        if (await context.Auctions.AnyAsync(a => a.Title == OpenAndClosed[0].Title))
            return;

        // Existing users are reused (by username), missing ones are created.
        var existing = await context.Users.ToDictionaryAsync(u => u.Username);
        var users = MarketplaceUsernames
            .Select(name => existing.TryGetValue(name, out var user)
                ? user
                : new User { Username = name, Email = $"{name}@example.com", PasswordHash = hash })
            .ToList();
        context.Users.AddRange(users.Where(u => u.Id == 0));

        var random = new Random(2026); // fixed seed - the same data every time
        var now = DateTime.UtcNow;
        var auctions = new List<Auction>();
        var bids = new List<Bid>();

        for (var i = 0; i < OpenAndClosed.Length; i++)
        {
            var demo = OpenAndClosed[i];
            var seller = users[i % users.Count];
            var auction = Active(seller, demo.Title, demo.Description, demo.StartingPrice, demo.DurationHours,
                now.AddHours(-demo.StartedHoursAgo));

            // Bids at increasing times and amounts, from users other than the seller.
            var lastBidTime = auction.EndsAt < now ? auction.EndsAt!.Value : now;
            var step = (lastBidTime - auction.ApprovedAt!.Value) / (demo.BidCount + 1);
            for (var b = 1; b <= demo.BidCount; b++)
            {
                var bidders = users.Where(u => u != seller).ToList();
                var bidder = bidders[random.Next(bidders.Count)];
                var raise = Math.Max(5, Math.Round(auction.CurrentPrice * (decimal)(0.03 + random.NextDouble() * 0.07) / 5) * 5);
                AddBid(bids, auction, bidder, auction.CurrentPrice + raise, auction.ApprovedAt.Value + step * b);
            }

            if (auction.EndsAt <= now)
            {
                auction.Status = AuctionStatus.Closed;
                auction.Winner = bids.LastOrDefault(b => b.Auction == auction)?.Bidder;
                auction.Version++;
            }
            auctions.Add(auction);
        }

        foreach (var (title, description, price, duration) in Pending)
        {
            auctions.Add(new Auction
            {
                Title = title, Description = description, StartingPrice = price, CurrentPrice = price,
                Status = AuctionStatus.Pending, CreatedAt = now.AddHours(-random.Next(1, 20)),
                DurationHours = duration, Seller = users[random.Next(users.Count)]
            });
        }

        foreach (var (title, description, price, duration, reason) in Rejected)
        {
            auctions.Add(new Auction
            {
                Title = title, Description = description, StartingPrice = price, CurrentPrice = price,
                Status = AuctionStatus.Rejected, RejectionReason = reason, CreatedAt = now.AddDays(-1),
                DurationHours = duration, Seller = users[random.Next(users.Count)], Version = 1
            });
        }

        // Favorites: each user follows a few open auctions of other sellers.
        var open = auctions.Where(a => a.Status == AuctionStatus.Active).ToList();
        var favorites = users
            .SelectMany(u => open.Where(a => a.Seller != u).OrderBy(_ => random.Next()).Take(2)
                .Select(a => new Favorite { User = u, Auction = a }))
            .ToList();

        context.Auctions.AddRange(auctions);
        context.Bids.AddRange(bids);
        context.Favorites.AddRange(favorites);
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
