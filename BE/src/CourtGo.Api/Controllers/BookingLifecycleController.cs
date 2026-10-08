using System.Security.Claims;
using CourtGo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CourtGo.Api.Controllers;
[ApiController, Route("api/staff/bookings"), Authorize(Roles = "Staff")]
public class BookingLifecycleController(IBookingLifecycleService service) : ControllerBase
{
    [HttpPost("{bookingId:guid}/no-show")]
    public async Task<IActionResult> MarkNoShowAsync(Guid bookingId, CancellationToken ct)
        => Ok(await service.MarkNoShowAsync(Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!), bookingId, ct));
}
