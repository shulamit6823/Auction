namespace Auction.Core.Entities;

public class Favorite
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int AuctionId { get; set; }
    public Auction Auction { get; set; } = null!;
}
