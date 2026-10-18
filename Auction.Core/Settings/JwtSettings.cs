namespace Auction.Core.Settings;

// Bound from the "Jwt" configuration section.
// Issuer, Audience and LifetimeMinutes come from appsettings.json;
// Key is a secret and comes from User Secrets (never from a file in the project).
public class JwtSettings
{
    public const string SectionName = "Jwt";
    public const int MinKeyLength = 32; // HmacSha256 needs a key of at least 256 bits

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int LifetimeMinutes { get; set; }
    public string Key { get; set; } = string.Empty;
}
