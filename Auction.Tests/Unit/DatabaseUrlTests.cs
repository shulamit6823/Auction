using Npgsql;

namespace Auction.Tests.Unit;

using Auction.Api.Configuration;

public class DatabaseUrlTests
{
    [Fact]
    public void RenderUrl_IsConvertedToNpgsqlFormat()
    {
        var result = new NpgsqlConnectionStringBuilder(
            DatabaseUrl.ToConnectionString("postgresql://auction:p%40ss@dpg-abc-a:5433/auction"));

        Assert.Equal("dpg-abc-a", result.Host);
        Assert.Equal(5433, result.Port);
        Assert.Equal("auction", result.Database);
        Assert.Equal("auction", result.Username);
        Assert.Equal("p@ss", result.Password); // %40 in a URL is "@"
    }

    [Fact]
    public void RenderUrl_WithoutPort_UsesDefaultPort()
    {
        var result = new NpgsqlConnectionStringBuilder(DatabaseUrl.ToConnectionString("postgres://u:p@host/db"));

        Assert.Equal(5432, result.Port);
    }

    [Fact]
    public void NpgsqlFormat_IsReturnedAsIs()
    {
        const string local = "Host=localhost;Port=5432;Database=auction;Username=postgres";

        Assert.Equal(local, DatabaseUrl.ToConnectionString(local));
    }
}
