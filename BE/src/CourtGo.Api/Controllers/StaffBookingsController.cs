using System.Security.Claims;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize(Roles = "Staff")]
public class StaffBookingsController(IStaffBookingService staffBookingService) : ControllerBase
{
    private readonly IStaffBookingService _staffBookingService = staffBookingService;

    [HttpPost("check-ins/verify")]
    public async Task<ActionResult<StaffQrVerificationResponse>> VerifyQrAsync(
        [FromBody] StaffQrVerificationRequest request, CancellationToken cancellationToken)
        => Ok(await _staffBookingService.VerifyQrAsync(GetUserId(), request, cancellationToken));

    [HttpPost("bookings/{bookingId:guid}/payments/remaining")]
    public async Task<ActionResult<CollectRemainingPaymentResponse>> CollectRemainingPaymentAsync(
        Guid bookingId, [FromBody] CollectRemainingPaymentRequest request, CancellationToken cancellationToken)
        => Ok(await _staffBookingService.CollectRemainingPaymentAsync(GetUserId(), bookingId, request, cancellationToken));

    [HttpPost("bookings/{bookingId:guid}/check-ins")]
    public async Task<ActionResult<CheckInResponse>> CheckInAsync(
        Guid bookingId, [FromBody] CreateCheckInRequest request, CancellationToken cancellationToken)
        => Ok(await _staffBookingService.CheckInAsync(GetUserId(), bookingId, request, cancellationToken));

    private Guid GetUserId()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id
            : throw new UnauthorizedException("User identity could not be verified from token.");
    }
}
