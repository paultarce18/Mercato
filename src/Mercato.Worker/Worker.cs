namespace Mercato.Worker;

/// <summary>
/// Placeholder background service from the project template. Replaced in Phase 8 by the
/// outbox publisher, the order saga and the scheduled jobs.
/// </summary>
public sealed class Worker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    private readonly ILogger<Worker> logger;

    public Worker(ILogger<Worker> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker heartbeat");
            await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
        }
    }
}
