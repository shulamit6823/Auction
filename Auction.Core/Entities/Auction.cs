namespace Auction.Core.Entities;

public class Auction
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }

    public AuctionStatus Status { get; set; } = AuctionStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public int DurationHours { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? EndsAt { get; set; }

    // Filled by the Admin when the auction is Rejected.
    public string? RejectionReason { get; set; }

    public int SellerId { get; set; }
    public User Seller { get; set; } = null!;

    public int? WinnerId { get; set; }
    public User? Winner { get; set; }

    // Concurrency token (Optimistic Concurrency): EF adds "WHERE Version = <value read>" to every
    // UPDATE. If someone else saved in between, no row matches and EF throws DbUpdateConcurrencyException.
    // Every change to an auction must increment it.
    public int Version { get; set; }
}
