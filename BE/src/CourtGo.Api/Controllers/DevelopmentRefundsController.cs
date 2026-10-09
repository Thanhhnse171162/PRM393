using CourtGo.Application.Bookings;
using CourtGo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CourtGo.Api.Controllers;
[ApiController, Route("api/dev/refunds"), Authorize(Roles = "Admin")]
public class DevelopmentRefundsController(IDevelopmentRefundSimulationService service, IHostEnvironment environment) : ControllerBase
{
    [HttpPost("{paymentId:guid}/simulate")]
    public async Task<IActionResult> SimulateAsync(Guid paymentId, SimulatePaymentRequest request, CancellationToken ct)
    {
        if (!environment.IsDevelopment()) return NotFound();
        return Ok(await service.SimulateAsync(paymentId, request, ct));
    }
}
