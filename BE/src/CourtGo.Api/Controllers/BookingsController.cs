using System.Security.Claims;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingHoldService _bookingHoldService;

    private readonly IBookingQueryService _bookingQueryService;

    public BookingsController(IBookingHoldService bookingHoldService, IBookingQueryService bookingQueryService)
    {
        _bookingHoldService = bookingHoldService;
        _bookingQueryService = bookingQueryService;
    }

    /// <summary>Creates a temporary reservation (hold) on one or more consecutive 1-hour slots for a customer.</summary>
    [HttpPost("hold")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(BookingHoldResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingHoldResponse>> Hold(
        [FromBody] BookingHoldRequest request,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId))
        {
            throw new UnauthorizedException("User identity could not be verified from token.");
        }

        var result = await _bookingHoldService.HoldAsync(userId, request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Placeholder endpoint so the route is visible in Swagger.</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { module = "bookings", status = "ok" });

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<PagedResult<BookingListItemDto>>> GetMyBookingsAsync(
        [FromQuery] string? statusGroup = "upcoming", [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await _bookingQueryService.GetMyBookingsAsync(GetUserId(), new(statusGroup, pageNumber, pageSize), cancellationToken));

    [HttpGet("{bookingId:guid}")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<BookingDetailDto>> GetBookingDetailAsync(
        Guid bookingId, CancellationToken cancellationToken)
        => Ok(await _bookingQueryService.GetBookingDetailAsync(GetUserId(), bookingId, cancellationToken));

    [HttpGet("{bookingId:guid}/qr")]
    [Authorize(Roles = "Customer")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<BookingQrResponse>> GetQrAsync(
        Guid bookingId, CancellationToken cancellationToken)
        => Ok(await _bookingQueryService.GetQrAsync(GetUserId(), bookingId, cancellationToken));

    private Guid GetUserId() => TryGetUserId(out var userId) ? userId
        : throw new UnauthorizedException("User identity could not be verified from token.");

    private bool TryGetUserId(out Guid userId)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out userId);
    }
}
