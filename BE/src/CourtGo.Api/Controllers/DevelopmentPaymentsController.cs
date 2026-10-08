using System.Reflection;
using System.Security.Claims;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/dev/payments")]
[Authorize(Roles = "Customer")]
public class DevelopmentPaymentsController(IDevelopmentPaymentSimulationService simulationService, IHostEnvironment environment)
    : ControllerBase
{
    private readonly IDevelopmentPaymentSimulationService _simulationService = simulationService;
    private readonly IHostEnvironment _environment = environment;

    /// <summary>Development simulation only. Does not charge MoMo/VNPay or transfer real money.</summary>
    [HttpPost("{paymentId:guid}/simulate")]
    public async Task<ActionResult<DepositConfirmationResponse>> SimulateAsync(Guid paymentId,
        [FromBody] SimulatePaymentRequest request, CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment()) return NotFound();
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var userId))
            throw new UnauthorizedException("User identity could not be verified from token.");
        return Ok(await _simulationService.SimulateAsync(userId, paymentId, request, cancellationToken));
    }
}

// Remove the route entirely (including MVC/Swagger discovery) outside Development.
public sealed class DevelopmentPaymentControllerFeatureProvider(bool isDevelopment) : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly bool _isDevelopment = isDevelopment;
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        if (!_isDevelopment)
            feature.Controllers.Remove(typeof(DevelopmentPaymentsController).GetTypeInfo());
    }
}
