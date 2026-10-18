namespace Auction.Core.Interfaces;

using Auction.Core.Entities;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}
