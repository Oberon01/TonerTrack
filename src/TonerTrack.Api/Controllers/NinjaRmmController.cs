using MediatR;
using Microsoft.AspNetCore.Mvc;
using TonerTrack.Application.NinjaRmm.Commands;

namespace TonerTrack.Api.Controllers;

[ApiController]
[Route("api/ninja")]
[Produces("application/json")]
public sealed class NinjaRmmController(IMediator mediator) : ControllerBase
{
    // POST /api/ninja/ticket
    [HttpPost("ticket")]
    [ProducesResponseType(typeof(CreateTicketResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateTicket(
        CreateTicketRequest body, CancellationToken ct)
    {
        var ticketId = await mediator.Send(new CreateTonerTicketCommand(
            body.ClientId, body.TicketFormId, body.LocationId, body.NodeId,
            body.Subject, body.Body), ct);

        return StatusCode(StatusCodes.Status201Created,
            new CreateTicketResponse(ticketId, "Ticket created successfully."));
    }
}

public sealed record CreateTicketRequest(
    int ClientId,
    int TicketFormId,
    int LocationId,
    int NodeId,
    string Subject,
    string Body);

public sealed record CreateTicketResponse(string TicketId, string Message);
