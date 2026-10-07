using CourtGo.Application.Courts;
using CourtGo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/courts")]
[AllowAnonymous]
public class CourtsController : ControllerBase
{
    private readonly ICourtService _courtService;

    public CourtsController(ICourtService courtService)
    {
        _courtService = courtService;
    }

    /// <summary>Lists active courts, optionally filtered by sport center or sport.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CourtDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourtDto>>> GetAll(
        [FromQuery] Guid? sportCenterId = null,
        [FromQuery] Guid? sportId = null,
        CancellationToken ct = default)
    {
        var courts = await _courtService.GetAllAsync(sportCenterId, sportId, ct);
        return Ok(courts);
    }

    /// <summary>Gets court details by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CourtDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourtDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        var court = await _courtService.GetByIdAsync(id, ct);
        return Ok(court);
    }

    /// <summary>Health check ping endpoint.</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { module = "courts", status = "ok" });
}
