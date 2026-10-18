using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Auction.Tests.Db;

using Auction.Data;

// The PostgreSQL tests use their own DB, "auction_tests" - never the development DB "auction",
// because they delete it and create it again.
// Host, user and password are taken from the Api's User Secrets (nothing secret in the code).
public static class PostgresTestDatabase
{
    public static string ConnectionString { get; } = BuildConnectionString();

    public static DbContextOptions<AuctionDbContext> Options { get; } = new DbContextOptionsBuilder<AuctionDbContext>()
        .UseNpgsql(ConnectionString)
        .Options;

    private static string BuildConnectionString()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing in the Api User Secrets.");

        return new NpgsqlConnectionStringBuilder(connectionString) { Database = "auction_tests" }.ConnectionString;
    }
}

// Runs once before all the PostgreSQL tests: a clean DB built by our real Migrations.
public class PostgresFixture
{
    public PostgresFixture()
    {
        using var context = new AuctionDbContext(PostgresTestDatabase.Options);
        context.Database.EnsureDeleted();
        context.Database.Migrate();
    }
}

// All the classes in this collection share the fixture and never run in parallel with each other.
[CollectionDefinition(Name)]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "PostgreSQL";
}
