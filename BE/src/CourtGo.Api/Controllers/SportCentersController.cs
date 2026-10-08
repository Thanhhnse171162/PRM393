using CourtGo.Application.Interfaces;
using CourtGo.Application.Reviews;
using CourtGo.Application.SportCenters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/sport-centers")]
[AllowAnonymous]
public class SportCentersController : ControllerBase
{
    private readonly ISportCenterService _sportCenterService;

    public SportCentersController(ISportCenterService sportCenterService)
    {
        _sportCenterService = sportCenterService;
    }

    /// <summary>Lists and filters active sport centers.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SportCenterSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SportCenterSummaryDto>>> GetAll(
        [FromQuery] Guid? sportId = null,
        [FromQuery] string? city = null,
        [FromQuery] string? district = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var centers = await _sportCenterService.GetAllAsync(sportId, city, district, search, ct);
        return Ok(centers);
    }

    /// <summary>Gets sport center details including active courts and operating hours.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SportCenterDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SportCenterDetailDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        var center = await _sportCenterService.GetByIdAsync(id, ct);
        return Ok(center);
    }

    /// <summary>Gets public reviews for a sport center.</summary>
    [HttpGet("{centerId:guid}/reviews")]
    public async Task<ActionResult<CenterReviewsResponse>> GetReviews(
        [FromRoute] Guid centerId,
        [FromServices] IReviewService reviewService,
        [FromQuery] CenterReviewQuery query,
        CancellationToken ct = default)
    {
        return Ok(await reviewService.GetCenterReviewsAsync(centerId, query, ct));
    }

    /// <summary>Health check ping endpoint.</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { module = "sport-centers", status = "ok" });
}
