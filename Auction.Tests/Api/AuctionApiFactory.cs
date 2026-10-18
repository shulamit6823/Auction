using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auction.Tests.Api;

using Auction.Api.BackgroundServices;
using Auction.Data;
using Auction.Tests.Db;

// Runs the real API in memory (real pipeline, real [Authorize] and real JWT checks).
// The DB is InMemory by default; PostgresApiFactory switches it to the PostgreSQL test DB.
public class AuctionApiFactory : WebApplicationFactory<Program>
{
    public AuctionApiFactory()
    {
        // A test-only key. Environment variables are read by WebApplication.CreateBuilder in Program.cs.
        Environment.SetEnvironmentVariable("Jwt__Key", "test-only-key-for-the-tests-0123456789-abcdefgh");
        // The tests create exactly the data they need - no demo data.
        Environment.SetEnvironmentVariable("Seed__Enabled", "false");
    }

    // One DB name per factory - the options are built again for every request.
    private readonly string _inMemoryDbName = Guid.NewGuid().ToString();

    protected virtual void ConfigureDatabase(DbContextOptionsBuilder options)
    {
        options.UseInMemoryDatabase(_inMemoryDbName);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.Remove(services.Single(d => d.ServiceType == typeof(DbContextOptions<AuctionDbContext>)));
            services.AddDbContext<AuctionDbContext>(ConfigureDatabase);

            // The tests close auctions themselves, at a known moment - not by the timer.
            services.Remove(services.Single(d => d.ImplementationType == typeof(AuctionClosingService)));
        });
    }
}

public class PostgresApiFactory : AuctionApiFactory
{
    protected override void ConfigureDatabase(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(PostgresTestDatabase.ConnectionString);
    }
}
