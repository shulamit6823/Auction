namespace Auction.Api.BackgroundServices;

using Auction.Core.Exceptions;
using Auction.Core.Interfaces;

public class AuctionClosingService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionClosingService> _logger;

    public AuctionClosingService(IServiceScopeFactory scopeFactory, ILogger<AuctionClosingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);

        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var auctionService = scope.ServiceProvider.GetRequiredService<IAuctionService>();

                var closedCount = await auctionService.CloseEndedAuctionsAsync(stoppingToken);
                if (closedCount > 0)
                    _logger.LogInformation("Closed {Count} ended auctions.", closedCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // The application is shutting down - not an error.
                break;
            }
            catch (ConcurrencyConflictException)
            {
                // An auction changed while we were closing it - not a bug. The next tick retries.
                _logger.LogWarning("Concurrency: an auction changed while closing ended auctions. Will retry in {Interval}.", CheckInterval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to close ended auctions.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
