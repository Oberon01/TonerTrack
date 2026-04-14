using MediatR;
using Microsoft.Extensions.Logging;
using TonerTrack.Application.Common.Interfaces;
using TonerTrack.Domain.Entities;
using TonerTrack.Domain.Repositories;

namespace TonerTrack.Application.Discovery;

public sealed record DiscoverPrintersCommand : IRequest<DiscoveryResult>;

public sealed record DiscoveryResult(
    int Added,
    int Updated,
    int Skipped,
    int TotalFound,
    IReadOnlyList<string> SourcesRun,
    IReadOnlyList<DiscoveryDetail> Details);

public sealed record DiscoveryDetail(
    string IpAddress,
    string Name,
    string Action,
    string Source);

public sealed class DiscoverPrintersHandler(
    IEnumerable<IPrinterDiscoveryService> discoveryServices,
    IPrinterRepository repo,
    ILogger<DiscoverPrintersHandler> logger)
    : IRequestHandler<DiscoverPrintersCommand, DiscoveryResult>
{
    public async Task<DiscoveryResult> Handle(
        DiscoverPrintersCommand _, CancellationToken ct)
    {
        var details = new List<DiscoveryDetail>();
        var sourcesRun = new List<string>();
        int added = 0, updated = 0, skipped = 0;

        foreach (var service in discoveryServices.Where(s => s.IsEnabled))
        {
            logger.LogInformation("Running discovery source: {Source}", service.SourceName);
            sourcesRun.Add(service.SourceName);

            IReadOnlyList<DiscoveredPrinter> found;
            try
            {
                found = await service.DiscoverAsync(ct);
                logger.LogInformation("{Source} found {Count} printer(s)",
                    service.SourceName, found.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Discovery source {Source} failed", service.SourceName);
                continue;
            }

            foreach (var discovered in found)
            {
                try
                {
                    var action = await UpsertAsync(discovered, ct);
                    details.Add(new DiscoveryDetail(
                        discovered.IpAddress, discovered.Name, action, service.SourceName));

                    switch (action)
                    {
                        case "added": added++; break;
                        case "updated": updated++; break;
                        case "skipped": skipped++; break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to upsert discovered printer {Ip}", discovered.IpAddress);
                }
            }
        }

        return new DiscoveryResult(added, updated, skipped, details.Count, sourcesRun, details);
    }

    private async Task<string> UpsertAsync(DiscoveredPrinter discovered, CancellationToken ct)
    {
        var existing = await repo.GetByIpAsync(discovered.IpAddress, ct);

        if (existing is null)
        {
            var printer = Printer.Create(
                discovered.IpAddress, discovered.Name, discovered.Community);
            printer.SetLocation(discovered.Location);
            await repo.AddAsync(printer, ct);
            logger.LogInformation("Discovered new printer: {Ip} ({Name}) via {Source}",
                discovered.IpAddress, discovered.Name, discovered.Source);
            return "added";
        }

        if (discovered.Source == "PrintServer")
        {
            existing.Rename(discovered.Name);
            existing.ClearUserOverride();
            existing.SetLocation(discovered.Location);
            await repo.UpdateAsync(existing, ct);
            logger.LogInformation("Updated printer {Ip}: name synced to '{Name}' from print server", discovered.IpAddress, discovered.Name);
            return "updated";
        }

        logger.LogDebug("Skipping name update for {Ip} from NetworkScan - printer already exists", discovered.IpAddress);
        return "skipped";
    }
}