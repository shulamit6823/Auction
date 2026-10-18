namespace Auction.Services;

using Auction.Core.Entities;

internal static class AuctionVisibility
{
    // Active and Closed auctions are public (also for guests).
    // A Pending or Rejected auction is not published - only its seller and an Admin may see it.
    public static bool CanView(Auction auction, int? viewerId, bool viewerIsAdmin)
    {
        return auction.Status is AuctionStatus.Active or AuctionStatus.Closed
            || viewerIsAdmin
            || auction.SellerId == viewerId;
    }
}
