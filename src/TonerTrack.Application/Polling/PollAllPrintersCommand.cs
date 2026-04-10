using MediatR;
using Microsoft.Extensions.Logging;
using TonerTrack.Application.Common.Interfaces;
using TonerTrack.Domain.Repositories;

namespace TonerTrack.Application.Polling;

public sealed record PollAllPrintersCommand : IRequest<PollAllResult>;

public sealed record PollAllResult(int Total, int Succeeded, int Failed, int Offline);

public sealed class PollAllPrintersHandler(
    IPrinterRepository repo,
    ISnmpService snmp,
    IDomainEventDispatcher dispatcher,
    IPrinterEnrichmentService enrichment,
    ILogger<PollAllPrintersHandler> logger)
    : IRequestHandler<PollAllPrintersCommand, PollAllResult>
{
    public async Task<PollAllResult> Handle(PollAllPrintersCommand _, CancellationToken ct)
    {
        var printers = await repo.GetAllAsync(ct);
        int succeeded = 0, failed = 0, offline = 0;

        // Bound concurrency — avoid flooding the network with too many simultaneous SNMP requests
        var semaphore = new SemaphoreSlim(10);

        var tasks = printers.Select(async printer =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var result = await snmp.PollPrinterAsync(printer.IpAddress, printer.Community, ct);

                if (result is null)
                {
                    printer.RecordSnmpUnreachable();
                    Interlocked.Increment(ref offline);
                    logger.LogWarning("SNMP unreachable: {Ip} ({Name})", printer.IpAddress, printer.Name);
                }
                else
                {
                    printer.ApplyPollResult(result);
                    Interlocked.Increment(ref succeeded);
                    logger.LogInformation("Polled {Ip} ({Name}) → {Status}",
                        printer.IpAddress, printer.Name, printer.Status);
                }

                // Enrich from print server
                var enriched = await enrichment.EnrichAsync(printer.IpAddress, ct);
                if (enriched is not null)
                {
                    if (!printer.Name.Equals(enriched.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        printer.Rename(enriched.Name);
                        printer.ClearUserOverride();
                    }
                    if (!string.IsNullOrWhiteSpace(enriched.Location))
                        printer.SetLocation(enriched.Location);
                }

                printer.RefreshEventNames();

                await repo.UpdateAsync(printer, ct);
                await dispatcher.DispatchAsync(printer.DomainEvents, ct);
                printer.ClearDomainEvents();
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failed);
                logger.LogError(ex, "Error polling {Ip}", printer.IpAddress);
            }
            finally { semaphore.Release(); }
        });

        await Task.WhenAll(tasks);
        return new PollAllResult(printers.Count, succeeded, failed, offline);
    }
}
