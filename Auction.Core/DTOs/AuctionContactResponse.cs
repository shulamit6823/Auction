namespace Auction.Core.DTOs;

// Email is personal information - this is returned only to the seller, the winner and an Admin
// (not part of AuctionResponse, which anyone can read).
// AutoMapper fills it from AuctionContact by name: SellerEmail <- Seller.Email, ...
public class AuctionContactResponse
{
    public string SellerUsername { get; set; } = string.Empty;
    public string SellerEmail { get; set; } = string.Empty;
    public string WinnerUsername { get; set; } = string.Empty;
    public string WinnerEmail { get; set; } = string.Empty;
}
