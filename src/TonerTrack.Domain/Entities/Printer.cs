using TonerTrack.Domain.Enums;
using TonerTrack.Domain.Events;
using TonerTrack.Domain.Exceptions;
using TonerTrack.Domain.ValueObjects;

namespace TonerTrack.Domain.Entities;

// Aggregate root representing a networked printer monitored via SNMP.
// All state mutation goes through public behavioural methods — never via setters.
public sealed class Printer
{
    // Printer identity
    public string IpAddress { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Community { get; private set; } = default!;
    public string Location { get; private set; } = string.Empty;
    public bool UserOverridden { get; private set; }
    public bool HasOpenTicket { get; private set; }

    // SNMP polled data
    public string Model { get; private set; } = "N/A";
    public string SerialNumber { get; private set; } = "N/A";
    public PrinterStatus Status { get; private set; } = PrinterStatus.Unknown;
    public DateTime? LastPolledAt { get; private set; }
    public long? TotalPagesPrinted { get; private set; }

    // Supplies
    private readonly List<Supply> _supplies = [];
    public IReadOnlyList<Supply> Supplies => _supplies.AsReadOnly();
    public IReadOnlyList<Supply> TonerCartridges => _supplies.Where(s => s.Category == SupplyCategory.TonerCartridge).ToList();
    public IReadOnlyList<Supply> DrumUnits => _supplies.Where(s => s.Category == SupplyCategory.DrumUnit).ToList();
    public IReadOnlyList<Supply> OtherSupplies => _supplies.Where(s => s.Category == SupplyCategory.Other).ToList();

    // Alerts
    private readonly List<PrinterAlert> _alerts = [];
    public IReadOnlyList<PrinterAlert> Alerts => _alerts.AsReadOnly();

    // For offline tracking
    public int OfflineAttempts { get; private set; }
    private const int OfflineThreshold = 3;

    // Page count history (key = "YYYY-MM", value = delta pages printed)
    private readonly Dictionary<string, long> _pagesHistory = [];
    public IReadOnlyDictionary<string, long> PagesHistory => _pagesHistory;
    public long? LastTotalPages { get; private set; }

    // Domain events 
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    private Printer() { }   // required by serialiser / ORM hydration

    public static Printer Create(string ipAddress, string name, string community = "public")
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new PrinterDomainException("IP address is required.");
        if (string.IsNullOrWhiteSpace(name))
            throw new PrinterDomainException("Printer name is required.");

        return new Printer
        {
            IpAddress = ipAddress.Trim(),
            Name = name.Trim(),
            Community = community.Trim(),
        };
    }

    // behavior/actions that can be performed on the aggregate
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new PrinterDomainException("Printer name cannot be empty.");

        Name = newName.Trim();
        UserOverridden = true;
    }

    public void SetCommunity(string community)
    {
        if (string.IsNullOrWhiteSpace(community))
            throw new PrinterDomainException("Community string cannot be empty.");

        Community = community.Trim();
    }

    public void SetLocation(string location) =>
        Location = location ?? string.Empty;

    public void ClearUserOverride() => UserOverridden = false;

    // Apply the result of a successful SNMP poll.
    // Computes new status, updates page history, and raises domain events.
    public void ApplyPollResult(PrinterPollResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var previousStatus = Status;
        OfflineAttempts = 0;
        Model = result.Model ?? "N/A";
        SerialNumber = result.SerialNumber ?? "N/A";
        LastPolledAt = DateTime.UtcNow;

        _supplies.Clear();
        _supplies.AddRange(result.Supplies);

        _alerts.Clear();
        _alerts.AddRange(result.Alerts);

        RecordPages(result.TotalPages);

        Status = EvaluateStatus();

        // Raise events after state is consistent

        var currentlyLow = HasLowToner();

        if (currentlyLow && !HasOpenTicket)
        {
            _domainEvents.Add(new PrinterTonerLowEvent(IpAddress, Name, Location, GetLowTonerSupplies()));
            HasOpenTicket = true;
        }

        if (!currentlyLow && HasOpenTicket)
        {
            HasOpenTicket = false;
        }

        if (previousStatus != Status && Status == PrinterStatus.Error)
            _domainEvents.Add(new PrinterStatusChangedEvent(IpAddress, Name, previousStatus, Status));
    }

    // Record that an SNMP poll attempt could not reach the device
    public void RecordSnmpUnreachable()
    {
        OfflineAttempts++;
        LastPolledAt = DateTime.UtcNow;

        if (OfflineAttempts >= OfflineThreshold)
            Status = PrinterStatus.Offline;
    }

    // Private helpers 
    // Status is driven by toner levels only (paper/errors are explicitly excluded)
    private PrinterStatus EvaluateStatus()
    {
        foreach (var toner in TonerCartridges)
        {
            if (toner.Level.IsCritical) return PrinterStatus.Error;
            if (toner.Level.IsLow) return PrinterStatus.Warning;
        }

        return PrinterStatus.Ok;
    }

    private bool HasLowToner() =>
        TonerCartridges.Any(t => t.Level.IsLow);

    private IReadOnlyList<Supply> GetLowTonerSupplies() =>
        TonerCartridges.Where(t => t.Level.IsLow).ToList();

    private void RecordPages(long? totalPages)
    {
        if (totalPages is null) return;

        TotalPagesPrinted = totalPages;

        if (LastTotalPages is null)
        {
            LastTotalPages = totalPages;
            return;
        }

        var delta = totalPages.Value - LastTotalPages.Value;
        if (delta < 0) { LastTotalPages = totalPages; return; }  // counter reset

        var monthKey = DateTime.UtcNow.ToString("yyyy-MM");
        _pagesHistory.TryGetValue(monthKey, out var existing);
        _pagesHistory[monthKey] = existing + delta;
        LastTotalPages = totalPages;
    }

    // Infrastructure hydration method to restore the aggregate from persistence.
    internal void RestoreFromPersistence(
        string location, bool userOverridden,
        string model, string serialNumber, PrinterStatus status,
        DateTime? lastPolledAt, long? totalPagesPrinted, long? lastTotalPages,
        int offlineAttempts,
        bool hasOpenTicket,
        IReadOnlyList<Supply> supplies,
        Dictionary<string, long> pagesHistory)
    {
        Location = location;
        UserOverridden = userOverridden;
        Model = model;
        SerialNumber = serialNumber;
        Status = status;
        LastPolledAt = lastPolledAt;
        TotalPagesPrinted = totalPagesPrinted;
        LastTotalPages = lastTotalPages;
        OfflineAttempts = offlineAttempts;
        HasOpenTicket = hasOpenTicket;
        _supplies.Clear();
        _supplies.AddRange(supplies);
        _pagesHistory.Clear();
        foreach (var kv in pagesHistory) _pagesHistory[kv.Key] = kv.Value;
    }

    public void RefreshEventNames()
    {
        for (var i = 0; i < _domainEvents.Count; i++)
        {
            _domainEvents[i] = _domainEvents[i] switch
            {
                PrinterTonerLowEvent e => e with { PrinterName = Name },
                PrinterStatusChangedEvent e => e with { PrinterName = Name },
                _ => _domainEvents[i]
            };
        }
    }
}

// DTO carrying raw SNMP poll results into the aggregate
public sealed record PrinterPollResult(
    string? Model,
    string? SerialNumber,
    long? TotalPages,
    IReadOnlyList<Supply> Supplies,
    IReadOnlyList<PrinterAlert> Alerts);
