namespace Auction.Core.Interfaces;

using Auction.Core.Entities;

public interface IBidRepository
{
    Task<IEnumerable<Bid>> GetByAuctionAsync(int auctionId, CancellationToken cancellationToken = default);
    // All bids of one user, with their auctions.
    Task<IEnumerable<Bid>> GetByBidderAsync(int bidderId, CancellationToken cancellationToken = default);
    Task<Bid?> GetHighestBidAsync(int auctionId, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);

    // Only marks the entity in the Change Tracker (no DB access) - stays sync.
    void Add(Bid bid);
}
