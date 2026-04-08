using MediatR;
using TonerTrack.Application.Common.Interfaces;

namespace TonerTrack.Application.NinjaRmm.Commands;

// Create a ticket in active ticketing system for toner-related issues (e.g. low toner, toner out, etc.) for a specific printer/node.
public sealed record CreateTonerTicketCommand(
    int ClientId,
    int TicketFormId,
    int LocationId,
    string Subject,
    string Body) : IRequest<string>;   // returns ticket reference / ID

public sealed class CreateTonerTicketHandler(INinjaRmmService ninja)
    : IRequestHandler<CreateTonerTicketCommand, string>
{
    public Task<string> Handle(CreateTonerTicketCommand cmd, CancellationToken ct) =>
        ninja.CreateTonerTicketAsync(
            cmd.ClientId, cmd.TicketFormId, cmd.LocationId,
            cmd.Subject, cmd.Body, ct
        );
}
