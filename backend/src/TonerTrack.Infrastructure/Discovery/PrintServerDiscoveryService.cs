using System;
using System.Collections.Generic;
using System.Net;
using System.Printing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TonerTrack.Application.Common.Interfaces;

namespace TonerTrack.Infrastructure.Discovery;

/// <summary>
/// Discovers printers by querying Windows print servers via System.Printing.
/// Extracts the IP address from each printer's standard TCP/IP port.
/// Only printers with a resolvable IP port are returned.
/// </summary>
public sealed class PrintServerDiscoveryService(
    IOptions<DiscoveryOptions> opts,
    ILogger<PrintServerDiscoveryService> logger)
    : IPrinterDiscoveryService
{
    private readonly PrintServerOptions _printServer = opts.Value.PrintServer;

    public string SourceName => "PrintServer";
    public bool IsEnabled => _printServer.Enabled;

    public async Task<IReadOnlyList<DiscoveredPrinter>> DiscoverAsync(CancellationToken ct = default)
    {
        var results = new List<DiscoveredPrinter>();

        foreach (var serverName in _printServer.ServerNames)
        {
            try
            {
                var printers = await QueryServerAsync(serverName, ct);
                results.AddRange(printers);
                logger.LogInformation(
                    "Print server {Server} returned {Count} printer(s)",
                    serverName, printers.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to query print server {Server}", serverName);
            }
        }

        return results;
    }

    private Task<IReadOnlyList<DiscoveredPrinter>> QueryServerAsync(
        string serverName, CancellationToken ct) =>
        Task.Run<IReadOnlyList<DiscoveredPrinter>>(() =>
        {
            var discovered = new List<DiscoveredPrinter>();

            using var server = new PrintServer(serverName);

            using var queue = server.GetPrintQueues(new[]
            {
                EnumeratedPrintQueueTypes.Connections,
                EnumeratedPrintQueueTypes.Local,
            });

            foreach (var printer in queue)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var ip = ExtractIpFromPortName(printer.QueuePort?.Name);
                    if (ip is null)
                    {
                        logger.LogTrace(
                            "Skipping {Printer} — could not extract IP from port '{Port}'",
                            printer.Name, printer.QueuePort?.Name);
                        continue;
                    }

                    var location = ResolveLocation(ip);

                    discovered.Add(new DiscoveredPrinter(
                        IpAddress: ip,
                        Name: printer.Name,
                        Location: location,
                        Community: "public",
                        Source: SourceName));

                    logger.LogDebug(
                        "Found printer: {Name} at {Ip} (Location: {Location})",
                        printer.Name, ip, location);
                }
                catch (Exception ex)
                {
                    logger.LogTrace(ex, "Error processing printer {Name}", printer.Name);
                }
                finally
                {
                    printer.Dispose();
                }
            }

            return discovered;
        }, ct);

    private static string? ExtractIpFromPortName(string? portName)
    {
        if (string.IsNullOrWhiteSpace(portName)) return null;

        Console.WriteLine($"[DEBUG] Raw port name: '{portName}'");
        var cleaned = portName
            .Replace("IP_", "")
            .Replace("TCP_", "")
            .Trim();

        return IPAddress.TryParse(cleaned, out _) ? cleaned : null;
    }

    private string ResolveLocation(string ip)
    {
        if (!IPAddress.TryParse(ip, out var address)) return "";

        var ipBytes = address.GetAddressBytes();
        var ipInt = (uint)(
            (ipBytes[0] << 24) | (ipBytes[1] << 16) |
            (ipBytes[2] << 8) | ipBytes[3]);

        foreach (var range in _printServer.LocationRanges)
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