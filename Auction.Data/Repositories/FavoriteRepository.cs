using Microsoft.EntityFrameworkCore;

namespace Auction.Data.Repositories;

using Auction.Core.Entities;
using Auction.Core.Interfaces;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly AuctionDbContext _context;

    public FavoriteRepository(AuctionDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Favorite>> GetByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Favorites
            .Include(f => f.Auction)
            .Where(f => f.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Favorite?> GetAsync(int userId, int auctionId, CancellationToken cancellationToken = default)
    {
        return await _context.Favorites.FindAsync(new object[] { userId, auctionId }, cancellationToken);
    }

    public void Add(Favorite favorite)
    {
        _context.Favorites.Add(favorite);
    }

    public void Remove(Favorite favorite)
    {
        _context.Favorites.Remove(favorite);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
