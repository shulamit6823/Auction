using Npgsql;

namespace Auction.Api.Configuration;

// Render gives the DB address as a URL: postgresql://user:password@host:port/database
// Npgsql needs the "Host=...;Username=..." format, so the URL is converted.
// A connection string that is already in the Npgsql format (local development) is returned as is.
public static class DatabaseUrl
{
    public static string ToConnectionString(string value)
    {
        if (!value.StartsWith("postgres://") && !value.StartsWith("postgresql://"))
            return value;

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port < 0 ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            SslMode = SslMode.Prefer
        }.ConnectionString;
    }
}
