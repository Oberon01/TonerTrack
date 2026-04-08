using TonerTrack.Domain.Enums;
using TonerTrack.Domain.ValueObjects;

namespace TonerTrack.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}

public sealed record PrinterTonerLowEvent(
    string IpAddress,
    string PrinterName,
    string Location,
    IReadOnlyList<Supply> LowSupplies) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record PrinterStatusChangedEvent(
    string IpAddress,
    string PrinterName,
    PrinterStatus Previous,
    PrinterStatus Current) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
