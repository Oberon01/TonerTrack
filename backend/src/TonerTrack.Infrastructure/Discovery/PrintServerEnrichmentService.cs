using System.Net;
using System.Printing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TonerTrack.Application.Common.Interfaces;

namespace TonerTrack.Infrastructure.Discovery;

/// <summary>
/// Looks up a printer by IP across all configured print servers and
/// returns its queue name and resolved location if found.
/// </summary>
public sealed class PrintServerEnrichmentService(
    IOptions<DiscoveryOptions> opts,
    ILogger<PrintServerEnrichmentService> logger)
    : IPrinterEnrichmentService
{
    private readonly PrintServerOptions _opts = opts.Value.PrintServer;

    public async Task<PrinterEnrichmentResult?> EnrichAsync(
        string ipAddress, CancellationToken ct = default)
    {
        if (!_opts.Enabled || _opts.ServerNames.Count == 0)
            return null;

        foreach (var serverName in _opts.ServerNames)
        {
            try
            {
                var result = await QueryServerAsync(serverName, ipAddress, ct);
                if (result is not null)
                {
                    logger.LogDebug(
                        "Enriched {Ip} from {Server} — name: '{Name}', location: '{Location}'",
                        ipAddress, serverName, result.Name, result.Location);
                    return result;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to query print server {Server} during enrichment", serverName);
            }
        }

        return null;
    }

    private Task<PrinterEnrichmentResult?> QueryServerAsync(
        string serverName, string ipAddress, CancellationToken ct) =>
        Task.Run<PrinterEnrichmentResult?>(() =>
        {
            using var server = new PrintServer(serverName);
            using var queues = server.GetPrintQueues(new[]
            {
                EnumeratedPrintQueueTypes.Local,
            });

            foreach (var queue in queues)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var portIp = ExtractIp(queue.QueuePort?.Name);
                    if (portIp is null ||
                        !portIp.Equals(ipAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        queue.Dispose();
                        continue;
                    }

                    var name = queue.Name;
                    var location = ResolveLocation(ipAddress);
                    queue.Dispose();
                    return new PrinterEnrichmentResult(name, location);
                }
                catch
                {
                    queue.Dispose();
                }
            }

            return null;
        }, ct);

    private static string? ExtractIp(string? portName)
    {
        if (string.IsNullOrWhiteSpace(portName)) return null;
        var cleaned = portName.Replace("IP_", "").Replace("TCP_", "").Trim();
        return IPAddress.TryParse(cleaned, out _) ? cleaned : null;
    }

    private string ResolveLocation(string ip)
    {
        if (!IPAddress.TryParse(ip, out var address)) return "";

        var ipBytes = address.GetAddressBytes();
        var ipInt = (uint)(
            (ipBytes[0] << 24) | (ipBytes[1] << 16) |
            (ipBytes[2] << 8) | ipBytes[3]);

        foreach (var range in _opts.LocationRanges)
        {
            if (!IPAddress.TryParse(range.Subnet, out var subnetAddr)) continue;

            var subnetBytes = subnetAddr.GetAddressBytes();
            var subnetInt = (uint)(
                (subnetBytes[0] << 24) | (subnetBytes[1] << 16) |
                (subnetBytes[2] << 8) | subnetBytes[3]);

            var mask = range.Cidr == 0 ? 0u : 0xFFFFFFFF << (32 - range.Cidr);
            var network = subnetInt & mask;

            if ((ipInt & mask) == network)
                return range.Location;
        }

        return "";
    }
}