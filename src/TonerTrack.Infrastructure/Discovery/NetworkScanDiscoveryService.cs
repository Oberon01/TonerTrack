using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TonerTrack.Application.Common.Interfaces;

namespace TonerTrack.Infrastructure.Discovery;

/// <summary>
/// Discovers printers by scanning configured IP ranges via SNMP.
/// Only hosts that respond to the Printer MIB sysDescr OID are imported.
/// </summary>
public sealed class NetworkScanDiscoveryService(
    IOptions<DiscoveryOptions> opts,
    ILogger<NetworkScanDiscoveryService> logger)
    : IPrinterDiscoveryService
{
    private const string OidSysDescr = "1.3.6.1.2.1.1.1.0";
    private const string OidModel = "1.3.6.1.2.1.25.3.2.1.3.1";
    private const VersionCode Version = VersionCode.V1;

    private readonly NetworkScanOptions _scan = opts.Value.NetworkScan;

    public string SourceName => "NetworkScan";
    public bool IsEnabled => _scan.Enabled;

    public async Task<IReadOnlyList<DiscoveredPrinter>> DiscoverAsync(CancellationToken ct = default)
    {
        var results = new System.Collections.Concurrent.ConcurrentBag<DiscoveredPrinter>();
        var semaphore = new SemaphoreSlim(_scan.MaxParallelism);
        var tasks = new List<Task>();

        foreach (var range in _scan.Ranges)
        {
            var addresses = ExpandCidr(range.Subnet, range.Cidr);
            logger.LogInformation(
                "Scanning {Count} addresses in {Subnet}/{Cidr} (Location: {Location})",
                addresses.Count, range.Subnet, range.Cidr, range.Name);

            foreach (var ip in addresses)
            {
                await semaphore.WaitAsync(ct);
                var capturedIp = ip;
                var capturedRange = range;

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var printer = await ProbeAsync(capturedIp, capturedRange, ct);
                        if (printer is not null) results.Add(printer);
                    }
                    finally { semaphore.Release(); }
                }, ct));
            }
        }

        await Task.WhenAll(tasks);
        logger.LogInformation("Network scan complete — {Count} printer(s) found", results.Count);
        return results.ToList();
    }

    private async Task<DiscoveredPrinter?> ProbeAsync(
        string ip, ScanRange range, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            try
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(ip), 161);
                var variables = new List<Variable>
                {
                    new(new ObjectIdentifier(OidSysDescr))
                };

                var result = Messenger.Get(Version, endpoint,
                    new OctetString(_scan.Community), variables, _scan.TimeoutMs);

                if (result.Count == 0) return null;
                var val = result[0].Data;
                if (val is NoSuchObject or NoSuchInstance) return null;

                var name = TryGetModel(ip) ?? ip;

                return new DiscoveredPrinter(
                    IpAddress: ip,
                    Name: name,
                    Location: range.Location,
                    Community: _scan.Community,
                    Source: SourceName);
            }
            catch (Lextm.SharpSnmpLib.Messaging.TimeoutException) { return null; }
            catch (SocketException) { return null; }
            catch (Exception ex)
            {
                logger.LogTrace(ex, "SNMP probe failed for {Ip}", ip);
                return null;
            }
        }, ct);
    }

    private string? TryGetModel(string ip)
    {
        try
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(ip), 161);
            var variables = new List<Variable> { new(new ObjectIdentifier(OidModel)) };
            var result = Messenger.Get(Version, endpoint,
                new OctetString(_scan.Community), variables, _scan.TimeoutMs);

            if (result.Count == 0) return null;
            var val = result[0].Data;
            return val is NoSuchObject or NoSuchInstance ? null : val.ToString();
        }
        catch { return null; }
    }

    private static IReadOnlyList<string> ExpandCidr(string subnet, int cidr)
    {
        var baseIp = IPAddress.Parse(subnet);
        var baseBytes = baseIp.GetAddressBytes();
        var baseInt = (uint)(
            (baseBytes[0] << 24) | (baseBytes[1] << 16) |
            (baseBytes[2] << 8) | baseBytes[3]);

        var mask = cidr == 0 ? 0u : 0xFFFFFFFF << (32 - cidr);
        var network = baseInt & mask;
        var broadcast = network | ~mask;

        var addresses = new List<string>();
        for (var i = network + 1; i < broadcast; i++)
        {
            addresses.Add(new IPAddress(new[]
            {
                (byte)((i >> 24) & 0xFF),
                (byte)((i >> 16) & 0xFF),
                (byte)((i >> 8) & 0xFF),
                (byte)(i & 0xFF),
            }).ToString());
        }

        return addresses;
    }
}