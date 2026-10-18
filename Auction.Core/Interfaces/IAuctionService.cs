namespace Auction.Core.Interfaces;

using Auction.Core.Entities;
using Auction.Core.Models;

public interface IAuctionService
{
    Task<IEnumerable<Auction>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Auction>> GetActiveAsync(string? search = null, AuctionSort sort = AuctionSort.EndingSoon, CancellationToken cancellationToken = default);
    Task<IEnumerable<Auction>> GetBySellerAsync(int sellerId, CancellationToken cancellationToken = default);
    Task<Auction> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Auction> GetVisibleByIdAsync(int id, int? viewerId, bool viewerIsAdmin, CancellationToken cancellationToken = default);
    Task<Auction> CreateAsync(int sellerId, string title, string description, decimal startingPrice, int durationAmount, DurationUnit durationUnit);

    // Seller only, while the auction is Pending or Rejected. A Rejected auction goes back to Pending.
    Task<Auction> UpdateAsync(int id, int sellerId, string title, string description, decimal startingPrice, int durationAmount, DurationUnit durationUnit);

    // Seller only, while the auction is Pending or Rejected (nobody could bid yet) - removes it.
    Task CancelAsync(int id, int sellerId);

    Task<Auction> ApproveAsync(int id);
    Task<Auction> RejectAsync(int id, string reason);
    Task DeleteAsync(int id);
    Task<int> CloseEndedAuctionsAsync(CancellationToken cancellationToken = default);
}
