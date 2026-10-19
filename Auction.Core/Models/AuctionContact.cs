namespace Auction.Core.Models;

using Auction.Core.Entities;

// The two sides of a closed auction, so they can contact each other after it ends.
public class AuctionContact
{
    public User Seller { get; set; } = null!;
    public User Winner { get; set; } = null!;
}
