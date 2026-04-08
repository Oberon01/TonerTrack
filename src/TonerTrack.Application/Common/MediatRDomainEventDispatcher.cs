using MediatR;
using TonerTrack.Application.Common.Interfaces;
using TonerTrack.Application.DomainEventHandlers;
using TonerTrack.Domain.Events;

namespace TonerTrack.Application.Common;

/// <summary>
/// Bridges domain events to MediatR notifications so that any handler
/// registered in DI receives them.
/// </summary>
public sealed class MediatRDomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct)
    {
        foreach (var evt in events)
        {
            INotification? notification = evt switch
            {
                PrinterTonerLowEvent e => new PrinterTonerLowEventNotification(e),
                PrinterStatusChangedEvent e => new PrinterStatusChangedNotification(e),
                _ => null
            };

            if (notification is not null)
                await publisher.Publish(notification, ct);
        }
    }
}
