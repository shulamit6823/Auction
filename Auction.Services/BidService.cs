using Microsoft.Extensions.Logging;

namespace Auction.Services;

using Auction.Core.Entities;
using Auction.Core.Exceptions;
using Auction.Core.Interfaces;
using Auction.Core.Models;

public class BidService : IBidService
{
    private readonly IBidRepository _bidRepository;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<BidService> _logger;

    public BidService(IBidRepository bidRepository, IAuctionRepository auctionRepository, IUserRepository userRepository,
        ILogger<BidService> logger)
    {
        _bidRepository = bidRepository;
        _auctionRepository = auctionRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Bid>> GetByAuctionAsync(int auctionId, int? viewerId, bool viewerIsAdmin, CancellationToken cancellationToken = default)
    {
        var auction = await _auctionRepository.GetByIdAsync(auctionId, cancellationToken);
        if (auction == null || !AuctionVisibility.CanView(auction, viewerId, viewerIsAdmin))
            throw new NotFoundException($"מכירה {auctionId} לא נמצאה.");

        return await _bidRepository.GetByAuctionAsync(auctionId, cancellationToken);
    }

    public async Task<IEnumerable<UserBidSummary>> GetMyBidsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var bids = await _bidRepository.GetByBidderAsync(userId, cancellationToken);

        // One row per auction, with the user's highest bid on it.
        return bids
            .GroupBy(b => b.AuctionId)
            .Select(g => new UserBidSummary { Auction = g.First().Auction, MyHighestBid = g.Max(b => b.Amount) })
            .OrderBy(s => s.Auction.Status == AuctionStatus.Active ? 0 : 1)
            .ThenBy(s => s.Auction.EndsAt)
            .ToList();
    }

    public async Task<Bid> PlaceBidAsync(int auctionId, int bidderId, decimal amount, int version)
    {
        var auction = await _auctionRepository.GetByIdAsync(auctionId);
        if (auction == null)
            throw new NotFoundException($"מכירה {auctionId} לא נמצאה.");
        if (await _userRepository.GetByIdAsync(bidderId) == null)
            throw new NotFoundException($"משתמש {bidderId} לא נמצא.");
        if (auction.SellerId == bidderId)
            throw new BusinessRuleException("מוכר לא יכול להציע על המכירה שלו.");

        var now = DateTime.UtcNow;
        if (auction.Status != AuctionStatus.Active)
            throw new BusinessRuleException("המכירה אינה פתוחה להצעות.");
        // EndsAt is the real end of the auction. The closing background service
        // may not have updated Status to Closed yet, so Status alone is not enough.
        if (auction.EndsAt == null || now >= auction.EndsAt.Value)
            throw new BusinessRuleException("המכירה הסתיימה.");
        // The client decided on the amount by an older state of the auction.
        if (version != auction.Version)
        {
            _logger.LogWarning("Concurrency: bid of {Amount} by user {BidderId} on auction {AuctionId} rejected - client version {ClientVersion}, current version {CurrentVersion}.",
                amount, bidderId, auctionId, version, auction.Version);
            throw new ConcurrencyConflictException("המכירה השתנתה מאז שנטענה. טענו אותה מחדש ונסו שוב.");
        }
        if (amount <= auction.CurrentPrice)
            throw new BusinessRuleException($"ההצעה חייבת להיות גבוהה מהמחיר הנוכחי (₪{auction.CurrentPrice}).");

        var bid = new Bid
        {
            AuctionId = auctionId,
            BidderId = bidderId,
            Amount = amount,
            CreatedAt = now
        };
        auction.CurrentPrice = amount;
        // If another bid was saved since we read the auction, the DB Version no longer matches
        // and SaveAsync throws ConcurrencyConflictException - only one of them succeeds.
        auction.Version++;

        _bidRepository.Add(bid);
        _auctionRepository.Update(auction);
        try
        {
            await _bidRepository.SaveAsync();
        }
        catch (ConcurrencyConflictException)
        {
            // Two bids read the same version at the same time - the other one was saved first.
            _logger.LogWarning("Concurrency: bid of {Amount} by user {BidderId} on auction {AuctionId} lost - another bid was saved first (version {Version}).",
                amount, bidderId, auctionId, version);
            throw;
        }

        _logger.LogInformation("Bid {BidId} of {Amount} placed by user {BidderId} on auction {AuctionId}.",
            bid.Id, amount, bidderId, auctionId);
        return bid;
    }
}
