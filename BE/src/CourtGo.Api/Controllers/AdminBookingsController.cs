using System.Security.Claims;
using CourtGo.Application.AdminBookings;
using CourtGo.Application.Bookings;
using CourtGo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/admin/bookings")]
[Authorize(Roles = "Admin")]
public class AdminBookingsController(IAdminBookingService bookingService) : ControllerBase
{
    private Guid AdminUserId => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminBookingListItemResponse>>> GetBookingsAsync(
        [FromQuery] AdminBookingQuery query,
        CancellationToken ct)
    {
        return Ok(await bookingService.GetBookingsAsync(query, ct));
    }

    [HttpGet("{bookingId:guid}")]
    public async Task<ActionResult<AdminBookingDetailResponse>> GetBookingDetailAsync(
        Guid bookingId,
        CancellationToken ct)
    {
        return Ok(await bookingService.GetBookingDetailAsync(bookingId, ct));
    }

    [HttpPost("{bookingId:guid}/cancel")]
    public async Task<ActionResult<AdminCancelBookingResponse>> CancelBookingAsync(
        Guid bookingId,
        [FromBody] AdminCancelBookingRequest request,
        CancellationToken ct)
    {
        return Ok(await bookingService.CancelBookingAsync(AdminUserId, bookingId, request, ct));
    }
}
