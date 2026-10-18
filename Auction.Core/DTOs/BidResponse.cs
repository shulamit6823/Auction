namespace Auction.Core.DTOs;

public class BidResponse
{
    public int Id { get; set; }
    public int AuctionId { get; set; }
    public int BidderId { get; set; }
    public string? BidderUsername { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
