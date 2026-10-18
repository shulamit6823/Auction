namespace Auction.Core.Interfaces;

using Auction.Core.Entities;
using Auction.Core.Models;

public interface IBidService
{
    Task<IEnumerable<Bid>> GetByAuctionAsync(int auctionId, int? viewerId, bool viewerIsAdmin, CancellationToken cancellationToken = default);
    // The auctions the user bid on: open ones first, then by end time.
    Task<IEnumerable<UserBidSummary>> GetMyBidsAsync(int userId, CancellationToken cancellationToken = default);
    Task<Bid> PlaceBidAsync(int auctionId, int bidderId, decimal amount, int version);
}
