namespace Auction.Core.Models;

// Sort options for the active auctions list (GET api/auctions?sort=PriceLow).
public enum AuctionSort
{
    EndingSoon,
    Newest,
    PriceLow,
    PriceHigh
}
