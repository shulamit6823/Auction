using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auction.Services;

using Auction.Core.Entities;
using Auction.Core.Interfaces;
using Auction.Core.Settings;

public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        // Only what the server needs to know who the user is - never the password or its hash.
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // read by CurrentUserId
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString())          // read by [Authorize(Roles = ...)]
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.LifetimeMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
