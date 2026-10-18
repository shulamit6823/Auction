using Microsoft.EntityFrameworkCore;

namespace Auction.Data.Repositories;

using Auction.Core.Entities;
using Auction.Core.Exceptions;
using Auction.Core.Interfaces;
using Auction.Core.Models;

public class AuctionRepository : IAuctionRepository
{
    private readonly AuctionDbContext _context;

    public AuctionRepository(AuctionDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Auction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Auctions
            .Include(a => a.Seller)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Auction>> GetByStatusAsync(AuctionStatus status, string? search = null, AuctionSort sort = AuctionSort.EndingSoon,
        CancellationToken cancellationToken = default)
    {
        // The query is built step by step and runs in the DB only at ToListAsync.
        var query = _context.Auctions
            .Include(a => a.Seller)
            .Where(a => a.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(a => a.Title.ToLower().Contains(term) || a.Description.ToLower().Contains(term));
        }

        query = sort switch
        {
            AuctionSort.Newest => query.OrderByDescending(a => a.ApprovedAt),
            AuctionSort.PriceLow => query.OrderBy(a => a.CurrentPrice),
            AuctionSort.PriceHigh => query.OrderByDescending(a => a.CurrentPrice),
            _ => query.OrderBy(a => a.EndsAt)
        };

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Auction>> GetBySellerAsync(int sellerId, CancellationToken cancellationToken = default)
    {
        return await _context.Auctions
            .Include(a => a.Seller)
            .Where(a => a.SellerId == sellerId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Auction>> GetActiveEndedBeforeAsync(DateTime time, CancellationToken cancellationToken = default)
    {
        return await _context.Auctions
            .Where(a => a.Status == AuctionStatus.Active && a.EndsAt <= time)
            .ToListAsync(cancellationToken);
    }

    public async Task<Auction?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Auctions
            .Include(a => a.Seller)
            .Include(a => a.Winner)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public void Add(Auction auction)
    {
        _context.Auctions.Add(auction);
    }

    public void Update(Auction auction)
    {
        _context.Auctions.Update(auction);
    }

    public void Remove(Auction auction)
    {
        _context.Auctions.Remove(auction);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("המכירה שונתה על ידי מישהו אחר. טענו אותה מחדש ונסו שוב.", ex);
        }
    }
}
