using TonerTrack.Domain.Entities;
using TonerTrack.Domain.Events;

namespace TonerTrack.Application.Common.Interfaces;

/// <summary>SNMP service abstraction.</summary>
public interface ISnmpService
{
    /// <summary>
    /// Poll a printer via SNMP. Returns null when the device is unreachable.
    /// </summary>
    Task<PrinterPollResult?> PollPrinterAsync(
        string ipAddress, string community, CancellationToken ct = default);
}

/// <summary>NinjaRMM ticketing abstraction.</summary>
public interface INinjaRmmService
{
    Task<string> CreateTonerTicketAsync(
        int clientId, int ticketFormId, int locationId, int nodeId,
        string subject, string body,
        CancellationToken ct = default);
}

/// <summary>
/// Dispatches domain events to interested handlers after the aggregate is persisted.
/// Implemented in Infrastructure using MediatR's IPublisher.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}
