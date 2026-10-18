namespace Auction.Core.Entities;

public enum AuctionStatus
{
    Pending,
    Active,
    Closed,
    Rejected // the Admin did not approve it; the seller can edit it and send it again (back to Pending)
}
