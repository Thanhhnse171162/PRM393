using System.Security.Claims;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CourtGo.Api.Controllers;
[ApiController, Route("api/staff"), Authorize(Roles = "Staff")]
public class StaffOperationsController(IStaffOperationsService service) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardAsync(CancellationToken ct) => Ok(await service.GetDashboardAsync(UserId, ct));
    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookingsAsync([FromQuery] StaffScheduleQuery query, CancellationToken ct)
        => Ok(await service.GetBookingsAsync(UserId, query, ct));
    [HttpGet("bookings/{bookingId:guid}")]
    public async Task<IActionResult> GetBookingAsync(Guid bookingId, CancellationToken ct)
        => Ok(await service.GetBookingAsync(UserId, bookingId, ct));
}
