namespace Auction.Core.Interfaces;

using Auction.Core.Entities;

public interface IFavoriteService
{
    Task<IEnumerable<Favorite>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task AddAsync(int userId, int auctionId);
    Task RemoveAsync(int userId, int auctionId);
}
