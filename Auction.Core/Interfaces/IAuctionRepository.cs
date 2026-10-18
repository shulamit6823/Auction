namespace Auction.Core.Interfaces;

using Auction.Core.Entities;
using Auction.Core.Models;

public interface IAuctionRepository
{
    Task<IEnumerable<Auction>> GetAllAsync(CancellationToken cancellationToken = default);
    // search: text in the title or description (null = no filter).
    Task<IEnumerable<Auction>> GetByStatusAsync(AuctionStatus status, string? search = null, AuctionSort sort = AuctionSort.EndingSoon,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Auction>> GetBySellerAsync(int sellerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Auction>> GetActiveEndedBeforeAsync(DateTime time, CancellationToken cancellationToken = default);
    Task<Auction?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);

    // Only mark the entity in the Change Tracker (no DB access) - stay sync.
    void Add(Auction auction);
    void Update(Auction auction);
    void Remove(Auction auction);
}
