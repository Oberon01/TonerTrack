using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TonerTrack.Application.Common.Interfaces;
using TonerTrack.Domain.Events;

namespace TonerTrack.Application.DomainEventHandlers;

// @TODO: Remove job specific details such as NinjaRMM and replace with a more generic ticketing system abstraction. This is just a demo of the concept.

// Notification types for domain events. 
public sealed record PrinterTonerLowEventNotification(PrinterTonerLowEvent Event)
    : INotification;

public sealed record PrinterStatusChangedNotification(PrinterStatusChangedEvent Event)
    : INotification;

// Automatic ticket creation handler for low toner events. More handlers can be added.

// Reacts to "PrinterTonerLowEventNotification" and creates a NinjaRMM ticket automatically.
// Configuration is supplied via "NinjaRmmTicketOptions" so values can be overridden per-environment
public sealed class TonerLowTicketHandler(
    INinjaRmmService logger_ninja,   // renamed param to satisfy primary-ctor
    IOptions<NinjaRmmTicketOptions> opts,
    ILogger<TonerLowTicketHandler> logger)
    : INotificationHandler<PrinterTonerLowEventNotification>
{
    public async Task Handle(PrinterTonerLowEventNotification notification, CancellationToken ct)
    {
        var evt = notification.Event;
        var o = opts.Value;

        var lowList = string.Join("\n",
            evt.LowSupplies.Select(s => $"  • {s.Name}: {s.Level.Display}"));

        var subject = $"Low Toner Alert – ({evt.Location}){evt.PrinterName}";
        var body = $"Printer {evt.PrinterName} ({evt.IpAddress}) has low toner:\n{lowList}";
        var locationId = int.TryParse(evt.Location, out var locId) ? locId : o.LocationId;

        try
        {
            var ticketRef = await logger_ninja.CreateTonerTicketAsync(
                o.ClientId, o.TicketFormId, o.LocationId,
                subject, body, ct);

            logger.LogInformation(
                "NinjaRMM ticket created for {Printer} ({Ip}): {Ref}",
                evt.PrinterName, evt.IpAddress, ticketRef);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to create NinjaRMM ticket for {Printer} ({Ip}). Response: {Message}",
                evt.PrinterName, evt.IpAddress, ex.Message);
        }
    }
}

// Options class for configuring ticket defaults when auto-creating tickets from domain events.

/// <summary>
/// Ticket defaults used when the domain event handler auto-creates tickets.
/// Configure via appsettings.json in "NinjaRmm:Ticketing".
/// </summary>
public sealed class NinjaRmmTicketOptions
{
    public const string Section = "NinjaRmm:Ticketing";

    public int ClientId { get; set; }
    public int TicketFormId { get; set; }
    public int LocationId { get; set; }
}
