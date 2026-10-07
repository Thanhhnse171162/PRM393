using CourtGo.Application.Interfaces;
using CourtGo.Application.Sports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/sports")]
[AllowAnonymous]
public class SportsController : ControllerBase
{
    private readonly ISportService _sportService;

    public SportsController(ISportService sportService)
    {
        _sportService = sportService;
    }

    /// <summary>Lists sports (active by default).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SportDto>>> GetAll(
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var sports = await _sportService.GetAllAsync(activeOnly, ct);
        return Ok(sports);
    }

    /// <summary>Gets sport details by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SportDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        var sport = await _sportService.GetByIdAsync(id, ct);
        return Ok(sport);
    }

    /// <summary>Health check ping endpoint.</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { module = "sports", status = "ok" });
}
