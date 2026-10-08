using System.Security.Claims;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CourtGo.Api.Controllers;
[ApiController, Route("api/bookings/{bookingId:guid}/cancellation-requests"), Authorize(Roles = "Customer")]
public class CancellationRequestsController(ICancellationService service) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpPost]
    public async Task<IActionResult> CreateAsync(Guid bookingId, CreateCancellationRequest request, CancellationToken ct)
        => Ok(await service.CreateRequestAsync(UserId, bookingId, request, ct));
    [HttpGet]
    public async Task<IActionResult> GetAsync(Guid bookingId, CancellationToken ct, int pageNumber = 1, int pageSize = 20)
        => Ok(await service.GetRequestsAsync(UserId, bookingId, pageNumber, pageSize, ct));
}
