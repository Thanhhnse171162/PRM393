using System.Security.Claims;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CourtGo.Api.Controllers;
[ApiController, Route("api/staff/bookings/walk-in"), Authorize(Roles = "Staff")]
public class WalkInBookingsController(IWalkInBookingService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateWalkInAsync(WalkInBookingRequest request, CancellationToken ct)
        => Ok(await service.CreateWalkInAsync(Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!), request, ct));
}
