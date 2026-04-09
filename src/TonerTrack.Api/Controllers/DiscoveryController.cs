using MediatR;
using Microsoft.AspNetCore.Mvc;
using TonerTrack.Application.Discovery;

namespace TonerTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class DiscoveryController(IMediator mediator) : ControllerBase
{
    // POST /api/discovery/run
    [HttpPost("run")]
    [ProducesResponseType(typeof(DiscoveryResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Run(CancellationToken ct) =>
        Ok(await mediator.Send(new DiscoverPrintersCommand(), ct));
}