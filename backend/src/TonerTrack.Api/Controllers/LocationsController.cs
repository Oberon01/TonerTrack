using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TonerTrack.Application.NinjaRmm;

namespace TonerTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class LocationsController(IOptions<LocationOptions> opts) : ControllerBase
{
    // GET /api/locations
    [HttpGet]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    public IActionResult GetAll() => Ok(opts.Value.Names);
}