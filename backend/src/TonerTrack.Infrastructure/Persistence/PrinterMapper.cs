using TonerTrack.Domain.Entities;
using TonerTrack.Domain.Enums;
using TonerTrack.Domain.ValueObjects;

namespace TonerTrack.Infrastructure.Persistence;

internal static class PrinterMapper
{
    /// <summary>Maps a domain entity to a storage record.</summary>
    public static PrinterRecord ToRecord(Printer p) => new()
    {
        IpAddress = p.IpAddress,
        Name = p.Name,
        Community = p.Community,
        Location = p.Location,
        UserOverridden = p.UserOverridden,
        Model = p.Model,
        SerialNumber = p.SerialNumber,
        Status = p.Status.ToString(),
        LastPolledAt = p.LastPolledAt,
        TotalPagesPrinted = p.TotalPagesPrinted,
        LastTotalPages = p.LastTotalPages,
        OfflineAttempts = p.OfflineAttempts,
        TonerCartridges = p.TonerCartridges.ToDictionary(s => s.Name, s => s.Level.Display),
        DrumUnits = p.DrumUnits.ToDictionary(s => s.Name, s => s.Level.Display),
        OtherSupplies = p.OtherSupplies.ToDictionary(s => s.Name, s => s.Level.Display),
        PagesHistory = new Dictionary<string, long>(p.PagesHistory),
        HasOpenTicket = p.HasOpenTicket,
    };

    /// <summary>Maps a storage record to a domain entity.</summary>
    public static Printer ToDomain(PrinterRecord r)
    {
        var printer = Printer.Create(r.IpAddress, r.Name, r.Community);

        // Restore persisted state via the internal reconstitution method.
        // This bypasses domain validation so it never re-raises events during hydration.
        printer.RestoreFromPersistence(
            location: r.Location,
            userOverridden: r.UserOverridden,
            model: r.Model,
            serialNumber: r.SerialNumber,
            status: Enum.TryParse<PrinterStatus>(r.Status, out var s) ? s : PrinterStatus.Unknown,
            lastPolledAt: r.LastPolledAt,
            totalPagesPrinted: r.TotalPagesPrinted,
            lastTotalPages: r.LastTotalPages,
            offlineAttempts: r.OfflineAttempts,
            hasOpenTicket: r.HasOpenTicket,
            supplies: BuildSupplies(r),
            pagesHistory: r.PagesHistory);

        return printer;
    }

    /// <summary>Helper method to build the list of supplies from the record's dictionaries.</summary>
    private static IReadOnlyList<Supply> BuildSupplies(PrinterRecord r)
    {
        var supplies = new List<Supply>();

        foreach (var (name, display) in r.TonerCartridges)
            supplies.Add(new Supply(name, SupplyLevel.FromDisplay(display), SupplyCategory.TonerCartridge));

        foreach (var (name, display) in r.DrumUnits)
            supplies.Add(new Supply(name, SupplyLevel.FromDisplay(display), SupplyCategory.DrumUnit));

        foreach (var (name, display) in r.OtherSupplies)
            supplies.Add(new Supply(name, SupplyLevel.FromDisplay(display), SupplyCategory.Other));

        return supplies;
    }
}
