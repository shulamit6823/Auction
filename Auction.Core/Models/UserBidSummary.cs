namespace Auction.Core.Models;

using Auction.Core.Entities;

// One auction the user bid on, with the user's highest bid on it ("My bids" page).
public class UserBidSummary
{
    public Auction Auction { get; set; } = null!;
    public decimal MyHighestBid { get; set; }

    // Every bid must be higher than the current price, so only the leading bid equals it.
    // On a closed auction this means the user won.
    public bool IsLeading => MyHighestBid == Auction.CurrentPrice;
}
