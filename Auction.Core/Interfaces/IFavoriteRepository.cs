namespace Auction.Core.Interfaces;

using Auction.Core.Entities;

public interface IFavoriteRepository
{
    Task<IEnumerable<Favorite>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<Favorite?> GetAsync(int userId, int auctionId, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);

    // Only mark the entity in the Change Tracker (no DB access) - stay sync.
    void Add(Favorite favorite);
    void Remove(Favorite favorite);
}
