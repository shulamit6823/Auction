using Microsoft.EntityFrameworkCore;

namespace Auction.Data;

using Auction.Core.Entities;

public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Auction> Auctions { get; set; } = null!;
    public DbSet<Bid> Bids { get; set; } = null!;
    public DbSet<Favorite> Favorites { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Two users cannot have the same username or email - even if two registrations arrive together.
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<Favorite>().HasKey(f => new { f.UserId, f.AuctionId });

        modelBuilder.Entity<Auction>().Property(a => a.Version).IsConcurrencyToken();
        modelBuilder.Entity<Auction>().Property(a => a.RejectionReason).HasMaxLength(500);
    }
}
