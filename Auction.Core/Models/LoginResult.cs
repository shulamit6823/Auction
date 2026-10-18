namespace Auction.Core.Models;

using Auction.Core.Entities;

public class LoginResult
{
    public User User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
