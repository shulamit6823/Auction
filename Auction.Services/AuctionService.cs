using Microsoft.Extensions.Logging;

namespace Auction.Services;

using Auction.Core.Entities;
using Auction.Core.Exceptions;
using Auction.Core.Interfaces;
using Auction.Core.Models;

public class AuctionService : IAuctionService
{
    private const int MaxDurationHours = 720; // one month = 30 days
    private const int MaxRejectionReasonLength = 500;

    private readonly IAuctionRepository _auctionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBidRepository _bidRepository;
    private readonly ILogger<AuctionService> _logger;

    public AuctionService(IAuctionRepository auctionRepository, IUserRepository userRepository, IBidRepository bidRepository,
        ILogger<AuctionService> logger)
    {
        _auctionRepository = auctionRepository;
        _userRepository = userRepository;
        _bidRepository = bidRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Auction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _auctionRepository.GetAllAsync(cancellationToken);
    }

    public async Task<IEnumerable<Auction>> GetActiveAsync(string? search = null, AuctionSort sort = AuctionSort.EndingSoon,
        CancellationToken cancellationToken = default)
    {
        var active = await _auctionRepository.GetByStatusAsync(AuctionStatus.Active, search, sort, cancellationToken);

        // The closing background service runs once a minute, so an auction may still be
        // Active after its EndsAt. It is already over - do not show it as open.
        var now = DateTime.UtcNow;
        return active.Where(a => a.EndsAt > now);
    }

    public async Task<IEnumerable<Auction>> GetBySellerAsync(int sellerId, CancellationToken cancellationToken = default)
    {
        return await _auctionRepository.GetBySellerAsync(sellerId, cancellationToken);
    }

    public async Task<Auction> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var auction = await _auctionRepository.GetByIdAsync(id, cancellationToken);
        if (auction == null)
            throw new NotFoundException($"מכירה {id} לא נמצאה.");

        return auction;
    }

    public async Task<Auction> GetVisibleByIdAsync(int id, int? viewerId, bool viewerIsAdmin, CancellationToken cancellationToken = default)
    {
        var auction = await GetByIdAsync(id, cancellationToken);

        // 404 and not 403: someone who may not see a pending auction should not learn that it exists.
        if (!AuctionVisibility.CanView(auction, viewerId, viewerIsAdmin))
            throw new NotFoundException($"מכירה {id} לא נמצאה.");

        return auction;
    }

    public async Task<Auction> CreateAsync(int sellerId, string title, string description, decimal startingPrice, int durationAmount, DurationUnit durationUnit)
    {
        var durationHours = ValidateDetails(title, startingPrice, durationAmount, durationUnit);
        if (await _userRepository.GetByIdAsync(sellerId) == null)
            throw new NotFoundException($"משתמש {sellerId} לא נמצא.");

        var auction = new Auction
        {
            Title = title.Trim(),
            Description = description,
            StartingPrice = startingPrice,
            CurrentPrice = startingPrice,
            Status = AuctionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            DurationHours = durationHours,
            SellerId = sellerId
        };

        _auctionRepository.Add(auction);
        await _auctionRepository.SaveAsync();

        _logger.LogInformation("Auction {AuctionId} created by user {SellerId}: starting price {StartingPrice}, {DurationHours} hours. Waiting for approval.",
            auction.Id, sellerId, startingPrice, durationHours);
        return auction;
    }

    public async Task<Auction> UpdateAsync(int id, int sellerId, string title, string description, decimal startingPrice, int durationAmount, DurationUnit durationUnit)
    {
        var auction = await GetOwnEditableAsync(id, sellerId);
        var durationHours = ValidateDetails(title, startingPrice, durationAmount, durationUnit);

        auction.Title = title.Trim();
        auction.Description = description;
        auction.StartingPrice = startingPrice;
        auction.CurrentPrice = startingPrice; // no bids yet - the auction was never active
        auction.DurationHours = durationHours;
        // An edited Rejected auction is sent to the Admin again.
        auction.Status = AuctionStatus.Pending;
        auction.RejectionReason = null;
        // If the Admin approves or rejects it at the same moment, only one of the two changes is saved.
        auction.Version++;

        _auctionRepository.Update(auction);
        await _auctionRepository.SaveAsync();

        _logger.LogInformation("Auction {AuctionId} edited by its seller {SellerId}. Waiting for approval.", id, sellerId);
        return auction;
    }

    public async Task CancelAsync(int id, int sellerId)
    {
        var auction = await GetOwnEditableAsync(id, sellerId);

        _auctionRepository.Remove(auction);
        await _auctionRepository.SaveAsync();

        _logger.LogInformation("Auction {AuctionId} cancelled by its seller {SellerId}.", id, sellerId);
    }

    // The seller may change their auction only before it was published (Pending or Rejected).
    private async Task<Auction> GetOwnEditableAsync(int id, int sellerId)
    {
        var auction = await GetByIdAsync(id);
        if (auction.SellerId != sellerId)
            throw new ForbiddenException("רק המוכר יכול לשנות את המכירה.");
        if (auction.Status is not (AuctionStatus.Pending or AuctionStatus.Rejected))
            throw new BusinessRuleException("אפשר לערוך או לבטל מכירה רק לפני שאושרה.");

        return auction;
    }

    // Shared by Create and Update. Returns the duration in hours.
    private static int ValidateDetails(string title, decimal startingPrice, int durationAmount, DurationUnit durationUnit)
    {
        var durationHours = ToHours(durationAmount, durationUnit);

        if (string.IsNullOrWhiteSpace(title))
            throw new BusinessRuleException("חובה להזין שם מוצר.");
        if (startingPrice < 0)
            throw new BusinessRuleException("מחיר הפתיחה לא יכול להיות שלילי.");
        if (durationHours > MaxDurationHours)
            throw new BusinessRuleException($"משך המכירה לא יכול להיות יותר מחודש ({MaxDurationHours} שעות).");

        return durationHours;
    }

    private static int ToHours(int amount, DurationUnit unit)
    {
        if (amount < 1 || amount > MaxDurationHours)
            throw new BusinessRuleException("משך המכירה חייב להיות מספר חיובי, עד חודש.");

        return unit switch
        {
            DurationUnit.Hours => amount,
            DurationUnit.Days => amount * 24,
            DurationUnit.Weeks => amount * 24 * 7,
            _ => throw new BusinessRuleException("יחידת זמן לא מוכרת.")
        };
    }

    public async Task<Auction> ApproveAsync(int id)
    {
        var auction = await GetByIdAsync(id);
        if (auction.Status != AuctionStatus.Pending)
            throw new BusinessRuleException("אפשר לאשר רק מכירה שממתינה לאישור.");

        var now = DateTime.UtcNow;
        auction.Status = AuctionStatus.Active;
        auction.ApprovedAt = now;
        auction.EndsAt = now.AddHours(auction.DurationHours);
        auction.Version++;

        _auctionRepository.Update(auction);
        await _auctionRepository.SaveAsync();

        _logger.LogInformation("Auction {AuctionId} approved. Active until {EndsAt:u}.", auction.Id, auction.EndsAt);
        return auction;
    }

    public async Task<Auction> RejectAsync(int id, string reason)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length == 0 || reason.Length > MaxRejectionReasonLength)
            throw new BusinessRuleException($"חובה לכתוב סיבת דחייה (עד {MaxRejectionReasonLength} תווים).");

        var auction = await GetByIdAsync(id);
        if (auction.Status != AuctionStatus.Pending)
            throw new BusinessRuleException("אפשר לדחות רק מכירה שממתינה לאישור.");

        auction.Status = AuctionStatus.Rejected;
        auction.RejectionReason = reason;
        auction.Version++;

        _auctionRepository.Update(auction);
        await _auctionRepository.SaveAsync();

        _logger.LogInformation("Auction {AuctionId} rejected by the Admin.", id);
        return auction;
    }

    public async Task DeleteAsync(int id)
    {
        var auction = await GetByIdAsync(id);
        if (auction.Status == AuctionStatus.Active)
            throw new BusinessRuleException("אי אפשר למחוק מכירה פעילה.");

        _auctionRepository.Remove(auction);
        await _auctionRepository.SaveAsync();

        _logger.LogInformation("Auction {AuctionId} deleted (status was {Status}).", id, auction.Status);
    }

    public async Task<int> CloseEndedAuctionsAsync(CancellationToken cancellationToken = default)
    {
        var ended = (await _auctionRepository.GetActiveEndedBeforeAsync(DateTime.UtcNow, cancellationToken)).ToList();

        // One await at a time: a DbContext does not support parallel operations.
        foreach (var auction in ended)
        {
            var highestBid = await _bidRepository.GetHighestBidAsync(auction.Id, cancellationToken);
            auction.WinnerId = highestBid?.BidderId;
            auction.Status = AuctionStatus.Closed;
            auction.Version++;
            _auctionRepository.Update(auction);
        }

        await _auctionRepository.SaveAsync(cancellationToken);

        // Logged only after the save succeeded - before it, nothing is really closed yet.
        foreach (var auction in ended)
        {
            _logger.LogInformation("Auction {AuctionId} closed. Winner: {WinnerId}, final price {FinalPrice}.",
                auction.Id, auction.WinnerId?.ToString() ?? "none", auction.CurrentPrice);
        }

        return ended.Count;
    }
}
