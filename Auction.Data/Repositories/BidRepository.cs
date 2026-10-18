using Microsoft.EntityFrameworkCore;

namespace Auction.Data.Repositories;

using Auction.Core.Entities;
using Auction.Core.Exceptions;
using Auction.Core.Interfaces;

public class BidRepository : IBidRepository
{
    private readonly AuctionDbContext _context;

    public BidRepository(AuctionDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Bid>> GetByAuctionAsync(int auctionId, CancellationToken cancellationToken = default)
    {
        return await _context.Bids
            .Include(b => b.Bidder)
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.Amount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Bid>> GetByBidderAsync(int bidderId, CancellationToken cancellationToken = default)
    {
        return await _context.Bids
            .Include(b => b.Auction)
            .Where(b => b.BidderId == bidderId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Bid?> GetHighestBidAsync(int auctionId, CancellationToken cancellationToken = default)
    {
        return await _context.Bids
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.Amount)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(Bid bid)
    {
        _context.Bids.Add(bid);
    }

    // Saves the new Bid together with the auction's new CurrentPrice and Version.
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("מישהו אחר הציע לפניך. טענו את המכירה מחדש ונסו שוב.", ex);
        }
    }
}
