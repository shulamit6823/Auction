namespace Auction.Core.DTOs;

using Auction.Core.Entities;

// AutoMapper fills the Auction* properties from UserBidSummary.Auction by name (flattening):
// AuctionTitle <- Auction.Title, AuctionCurrentPrice <- Auction.CurrentPrice, ...
public class MyBidResponse
{
    public int AuctionId { get; set; }
    public string AuctionTitle { get; set; } = string.Empty;
    public AuctionStatus AuctionStatus { get; set; }
    public decimal AuctionCurrentPrice { get; set; }
    public DateTime? AuctionEndsAt { get; set; }
    public decimal MyHighestBid { get; set; }
    public bool IsLeading { get; set; }
}
