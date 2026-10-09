using System.Security.Claims;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CourtGo.Api.Controllers;
[ApiController, Route("api/admin/cancellation-requests"), Authorize(Roles = "Admin")]
public class AdminCancellationRequestsController(ICancellationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] AdminCancellationRequestQuery query, CancellationToken ct)
        => Ok(await service.GetAdminRequestsAsync(query, ct));
    [HttpGet("{requestId:guid}")]
    public async Task<IActionResult> GetAsync(Guid requestId, CancellationToken ct) => Ok(await service.GetAdminRequestDetailAsync(requestId, ct));
    [HttpPost("{requestId:guid}/decision")]
    public async Task<IActionResult> ProcessDecisionAsync(Guid requestId, CancellationDecisionRequest request, CancellationToken ct)
        => Ok(await service.ProcessDecisionAsync(Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!), requestId, request, ct));
    [HttpPost("{requestId:guid}/decisions")]
    public async Task<IActionResult> DecideAsync(Guid requestId, CancellationDecisionRequest request, CancellationToken ct)
        => Ok(await service.DecideAsync(Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!), requestId, request, ct));
}
