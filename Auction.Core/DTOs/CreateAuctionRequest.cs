namespace Auction.Core.DTOs;

using Auction.Core.Entities;

public class CreateAuctionRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public int DurationAmount { get; set; }
    public DurationUnit DurationUnit { get; set; }
}
