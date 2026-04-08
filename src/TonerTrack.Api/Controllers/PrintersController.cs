using MediatR;
using Microsoft.AspNetCore.Mvc;
using TonerTrack.Application.Polling;
using TonerTrack.Application.Printers.Commands;
using TonerTrack.Application.Printers.DTOs;
using TonerTrack.Application.Printers.Queries;

namespace TonerTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class PrintersController(IMediator mediator) : ControllerBase
{
    // GET /api/printers
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllPrintersQuery(), ct);
        return Ok(new { printers = result });
    }

    // GET /api/printers/stats
    [HttpGet("stats")]
    [ProducesResponseType(typeof(PrinterStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Stats(CancellationToken ct) =>
        Ok(await mediator.Send(new GetPrinterStatsQuery(), ct));

    // GET /api/printers/{ip}
    [HttpGet("{ip}")]
    [ProducesResponseType(typeof(PrinterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string ip, CancellationToken ct) =>
        Ok(await mediator.Send(new GetPrinterByIpQuery(ip), ct));

    // GET /api/printers/{ip}/usage
    [HttpGet("{ip}/usage")]
    [ProducesResponseType(typeof(UsageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Usage(string ip, CancellationToken ct) =>
        Ok(await mediator.Send(new GetPrinterUsageQuery(ip), ct));

    // POST /api/printers
    [HttpPost]
    [ProducesResponseType(typeof(PrinterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Add(AddPrinterRequest body, CancellationToken ct)
    {
        var dto = await mediator.Send(
            new AddPrinterCommand(body.Name, body.IpAddress, body.Community ?? "public"), ct);
        return CreatedAtAction(nameof(Get), new { ip = dto.IpAddress }, dto);
    }

    // PUT /api/printers/{ip}
    [HttpPut("{ip}")]
    [ProducesResponseType(typeof(PrinterDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        string ip, UpdatePrinterRequest body, CancellationToken ct) =>
        Ok(await mediator.Send(new UpdatePrinterCommand(ip, body.Name, body.Community), ct));

    // DELETE /api/printers/{ip}
    [HttpDelete("{ip}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string ip, CancellationToken ct)
    {
        await mediator.Send(new DeletePrinterCommand(ip), ct);
        return NoContent();
    }

    // POST /api/printers/{ip}/poll
    [HttpPost("{ip}/poll")]
    [ProducesResponseType(typeof(PrinterDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Poll(string ip, CancellationToken ct) =>
        Ok(await mediator.Send(new PollPrinterCommand(ip), ct));

    // POST /api/printers/poll-all
    [HttpPost("poll-all")]
    [ProducesResponseType(typeof(PollAllResult), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> PollAll(CancellationToken ct) =>
        Accepted(await mediator.Send(new PollAllPrintersCommand(), ct));

    // POST /api/printers/import
    [HttpPost("import")]
    [ProducesResponseType(typeof(ImportPrintersResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Import(
        IReadOnlyList<ImportPrinterRequestBody> body, CancellationToken ct)
    {
        var command = new ImportPrintersCommand(
            body.Select(r => new ImportPrinterRequest(
                r.IpAddress, r.Name, r.Community ?? "public")).ToList());
        return Ok(await mediator.Send(command, ct));
    }
}

// Request body models
public sealed record AddPrinterRequest(string Name, string IpAddress, string? Community);
public sealed record UpdatePrinterRequest(string? Name, string? Community);
public sealed record ImportPrinterRequestBody(string IpAddress, string Name, string? Community);
