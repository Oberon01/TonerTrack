using TonerTrack.Domain.Entities;
using TonerTrack.Domain.Enums;

namespace TonerTrack.Application.Printers.DTOs;

public sealed record PrinterDto(
    string IpAddress,
    string Name,
    string Community,
    string Location,
    string Model,
    string SerialNumber,
    PrinterStatus Status,
    DateTime? LastPolledAt,
    string? TotalPagesPrinted,
    int OfflineAttempts,
    Dictionary<string, string> TonerCartridges,
    Dictionary<string, string> DrumUnits,
    Dictionary<string, string> OtherSupplies,
    Dictionary<string, string> MonthlyPages)
{
    public static PrinterDto FromEntity(Printer p) => new(
        IpAddress: p.IpAddress,
        Name: p.Name,
        Community: p.Community,
        Location: p.Location,
        Model: p.Model,
        SerialNumber: p.SerialNumber,
        Status: p.Status,
        LastPolledAt: p.LastPolledAt,
        TotalPagesPrinted: p.TotalPagesPrinted?.ToString(),
        OfflineAttempts: p.OfflineAttempts,
        TonerCartridges: p.TonerCartridges.ToDictionary(s => s.Name, s => s.Level.Display),
        DrumUnits: p.DrumUnits.ToDictionary(s => s.Name, s => s.Level.Display),
        OtherSupplies: p.OtherSupplies.ToDictionary(s => s.Name, s => s.Level.Display),
        MonthlyPages: p.PagesHistory.ToDictionary(kv => kv.Key, kv => kv.Value.ToString()));
}

public sealed record PrinterStatsDto(
    int Total, int Ok, int Warning, int Error, int Offline, int Unknown);

public sealed record UsageDto(
    string IpAddress,
    string Name,
    IReadOnlyList<MonthlyUsageDto> Last6Months,
    long AverageLast6,
    long? LastMonth,
    double? MonthChangePercent,
    IReadOnlyDictionary<string, long> FullHistory);

public sealed record MonthlyUsageDto(string Month, long Pages);
