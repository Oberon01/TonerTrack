using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TonerTrack.Application.Polling;

namespace TonerTrack.Infrastructure.BackgroundServices;

/// <summary>
/// Long-running <see cref="BackgroundService"/> that polls all printers on a
/// configurable interval. A fresh DI scope is created per cycle so each poll
/// gets its own unit of work and there are no stale singleton dependencies.
/// </summary>
public sealed class PrinterPollingBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<PollingOptions> opts,
    ILogger<PrinterPollingBackgroundService> logger)
    : BackgroundService
{
    private readonly PollingOptions _opts = opts.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opts.Enabled)
        {
            logger.LogInformation("Automatic printer polling is disabled.");
            return;
        }

        logger.LogInformation(
            "Automatic polling enabled — interval {Min} min, first poll in {Delay}s.",
            _opts.IntervalSeconds / 60, _opts.InitialDelaySeconds);

        // Small startup delay so the rest of the application is ready
        await Task.Delay(TimeSpan.FromSeconds(_opts.InitialDelaySeconds), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Auto-poll starting at {Time:O}", DateTime.UtcNow);

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(new PollAllPrintersCommand(), stoppingToken);

                logger.LogInformation(
                    "Auto-poll complete: total={Total} ok={Succeeded} offline={Offline} failed={Failed}",
                    result.Total, result.Succeeded, result.Offline, result.Failed);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in auto-poll cycle — retrying next interval.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_opts.IntervalSeconds), stoppingToken);
        }
    }
}

// Polling configuration options, bound from appsettings.json
public sealed class PollingOptions
{
    public const string Section = "Polling";

    public bool Enabled  { get; set; } = true;
    public int IntervalSeconds { get; set; } = 300;   // 5 minutes
    public int InitialDelaySeconds { get; set; } = 30;
}
