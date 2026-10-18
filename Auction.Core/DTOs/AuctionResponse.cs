namespace Auction.Core.DTOs;

using Auction.Core.Entities;

public class AuctionResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public AuctionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DurationHours { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? RejectionReason { get; set; }
    public int SellerId { get; set; }
    public string? SellerUsername { get; set; }
    public int? WinnerId { get; set; }

    // Sent back by the client in PlaceBidRequest (Optimistic Concurrency).
    public int Version { get; set; }
}
