using CourtGo.Application.Availability;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[AllowAnonymous]
public class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;

    public AvailabilityController(IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    /// <summary>Gets 1-hour availability slots for a court on a specified date (YYYY-MM-DD).</summary>
    [HttpGet("api/courts/{courtId:guid}/availability")]
    [ProducesResponseType(typeof(CourtAvailabilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourtAvailabilityDto>> GetAvailability(
        [FromRoute] Guid courtId,
        [FromQuery] DateOnly? date,
        CancellationToken ct = default)
    {
        if (!date.HasValue)
        {
            throw new ValidationException("Query parameter 'date' is required in YYYY-MM-DD format.", ErrorCodes.ValidationError);
        }

        var result = await _availabilityService.GetCourtAvailabilityAsync(courtId, date.Value, ct);
        return Ok(result);
    }

    /// <summary>Query-string alias for availability: GET /api/availability?courtId={courtId}&date={date}.</summary>
    [HttpGet("api/availability")]
    [ProducesResponseType(typeof(CourtAvailabilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourtAvailabilityDto>> GetAvailabilityByQuery(
        [FromQuery] Guid courtId,
        [FromQuery] DateOnly? date,
        CancellationToken ct = default)
    {
        if (courtId == Guid.Empty)
        {
            throw new ValidationException("Query parameter 'courtId' is required.", ErrorCodes.ValidationError);
        }

        if (!date.HasValue)
        {
            throw new ValidationException("Query parameter 'date' is required in YYYY-MM-DD format.", ErrorCodes.ValidationError);
        }

        var result = await _availabilityService.GetCourtAvailabilityAsync(courtId, date.Value, ct);
        return Ok(result);
    }

    /// <summary>Health check ping endpoint.</summary>
    [HttpGet("api/availability/ping")]
    public IActionResult Ping() => Ok(new { module = "availability", status = "ok" });
}
