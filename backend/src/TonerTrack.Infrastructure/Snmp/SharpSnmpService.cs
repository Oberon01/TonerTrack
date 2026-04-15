using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using TonerTrack.Application.Common.Interfaces;
using TonerTrack.Domain.Entities;
using TonerTrack.Domain.ValueObjects;

namespace TonerTrack.Infrastructure.Snmp;

/// <summary>
/// SNMP service using Lextm.SharpSnmpLib.
/// </summary>
public sealed class SharpSnmpService(ILogger<SharpSnmpService> logger) : ISnmpService
{
    // Standard printer MIB OIDs
    private const string OidSysDescr = "1.3.6.1.2.1.1.1.0"; // System description (used for reachability check)
    private const string OidModel = "1.3.6.1.2.1.25.3.2.1.3.1"; // Model name (HOST-RESOURCES-MIB)
    private const string OidSerial = "1.3.6.1.2.1.43.5.1.1.17.1"; // Serial number
    private const string OidPageCount = "1.3.6.1.2.1.43.10.2.1.4.1.1"; // Total page count
    private const string OidSupplyDescBase = "1.3.6.1.2.1.43.11.1.1.6.1"; // Supply description table base OID
    private const string OidSupplyLevelBase = "1.3.6.1.2.1.43.11.1.1.9.1"; // Supply level table base OID
    private const string OidSupplyMaxBase = "1.3.6.1.2.1.43.11.1.1.8.1"; // Supply max capacity table base OID
    private const string OidAlertDescBase = "1.3.6.1.2.1.43.18.1.1.8"; // Alert description table base OID
    private const string OidAlertSevBase = "1.3.6.1.2.1.43.18.1.1.2"; // Alert severity table base OID

    private const int SnmpPort = 161;
    private const int TimeoutMs = 2000;
    private const VersionCode Version = VersionCode.V1;

    public async Task<PrinterPollResult?> PollPrinterAsync(
        string ipAddress, string community, CancellationToken ct = default)
    {
        // Quick reachability check — if sysDescr returns nothing the device is offline
        var sysDescr = await SnmpGetAsync(ipAddress, community, OidSysDescr, ct);
        if (sysDescr is null)
        {
            logger.LogWarning("SNMP unreachable: {Ip}", ipAddress);
            return null;
        }

        var model  = await SnmpGetAsync(ipAddress, community, OidModel, ct) ?? "N/A";
        var serial = await SnmpGetAsync(ipAddress, community, OidSerial, ct) ?? "N/A";

        var pageCountStr = await SnmpGetAsync(ipAddress, community, OidPageCount, ct);
        long? pageCount  = long.TryParse(pageCountStr, out var pc) ? pc : null;

        var supplies = await BuildSuppliesAsync(ipAddress, community, ct);
        var alerts = await BuildAlertsAsync(ipAddress, community, ct);

        return new PrinterPollResult(model, serial, pageCount, supplies, alerts);
    }

    // Supply discovery
    /// <summary>Builds a list of supplies by querying the printer's SNMP interface.</summary>
    private async Task<IReadOnlyList<Supply>> BuildSuppliesAsync(
        string ip, string community, CancellationToken ct)
    {
        var supplies = new List<Supply>();
        var descTable = await SnmpWalkAsync(ip, community, OidSupplyDescBase, ct);

        foreach (var (oid, name) in descTable)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                name.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                continue;

            var index = oid.Split('.').Last();
            var levelStr = await SnmpGetAsync(ip, community, $"{OidSupplyLevelBase}.{index}", ct);
            var maxStr = await SnmpGetAsync(ip, community, $"{OidSupplyMaxBase}.{index}", ct);

            if (!int.TryParse(levelStr, out var level) || !int.TryParse(maxStr, out var max))
                continue;

            var supplyLevel = SupplyLevel.FromRaw(level, max);
            var category    = Supply.CategorizeByName(name);
            supplies.Add(new Supply(name.Trim(), supplyLevel, category));
        }

        return supplies;
    }

    /// <summary>Builds a list of active printer alerts by querying the printer's SNMP interface.</summary>
    private async Task<IReadOnlyList<PrinterAlert>> BuildAlertsAsync(
        string ip, string community, CancellationToken ct)
    {
        var alerts = new List<PrinterAlert>();
        var descTable = await SnmpWalkAsync(ip, community, OidAlertDescBase, ct);
        var sevTable  = await SnmpWalkAsync(ip, community, OidAlertSevBase,  ct);

        foreach (var (oid, desc) in descTable)
        {
            var suffix = oid.Replace($"{OidAlertDescBase}.", "");
            sevTable.TryGetValue($"{OidAlertSevBase}.{suffix}", out var sevCode);

            var severity = PrinterAlert.FromSnmpCode(sevCode ?? "");
            if (severity is AlertSeverity.Warning or AlertSeverity.Critical)
                alerts.Add(new PrinterAlert(desc, severity));
        }

        return alerts;
    }

    // Helper methods for SNMP GET and WALK operations, wrapped in Tasks for async usage.
    /// <summary>Performs an SNMP GET operation and returns the value as a string, or null if unreachable.</summary>
    private Task<string?> SnmpGetAsync(
        string ip, string community, string oid, CancellationToken ct) =>
        Task.Run(() =>
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex => ex is not Lextm.SharpSnmpLib.Messaging.TimeoutException)
                .WaitAndRetry(2, attempt => TimeSpan.FromMilliseconds(300 * attempt));
            try
            {
                return retryPolicy.Execute(() =>
                {
                    var endpoint = new IPEndPoint(IPAddress.Parse(ip), SnmpPort);
                    var variables = new List<Variable> { new(new ObjectIdentifier(oid)) };
                    var result = Messenger.Get(Version, endpoint,
                        new OctetString(community), variables, TimeoutMs);

                    if (result.Count == 0) return null;
                    var val = result[0].Data;
                    return val is NoSuchObject or NoSuchInstance ? null : val.ToString();
                });
            }
            catch (Lextm.SharpSnmpLib.Messaging.TimeoutException) { return null; }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "SNMP GET failed: {Ip} {Oid}", ip, oid);
                return null;
            }
        }, ct);

    /// <summary>Performs an SNMP WALK operation starting from the base OID and returns a dictionary of OID suffixes to values.</summary>
    private Task<Dictionary<string, string>> SnmpWalkAsync(
        string ip, string community, string baseOid, CancellationToken ct) =>
        Task.Run(() =>
        {
            var results = new Dictionary<string, string>();
            try
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(ip), SnmpPort);
                var walked = new List<Variable>();
                Messenger.Walk(Version, endpoint,
                    new OctetString(community),
                    new ObjectIdentifier(baseOid),
                    walked, TimeoutMs, WalkMode.WithinSubtree);

                foreach (var v in walked)
                    results[v.Id.ToString()] = v.Data.ToString() ?? "";
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "SNMP WALK failed: {Ip} {BaseOid}", ip, baseOid);
            }

            return results;
        }, ct);
}
