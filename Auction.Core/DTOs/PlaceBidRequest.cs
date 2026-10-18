namespace Auction.Core.DTOs;

public class PlaceBidRequest
{
    public decimal Amount { get; set; }

    // The auction Version the client saw (from AuctionResponse).
    public int Version { get; set; }
}
