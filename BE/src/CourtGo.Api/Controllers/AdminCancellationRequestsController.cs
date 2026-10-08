using System.Security.Claims;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/admin/cancellation-requests")]
[Authorize(Roles = "Admin")]
public class AdminCancellationRequestsController(ICancellationService cancellationService) : ControllerBase
{
    private Guid AdminUserId => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminCancellationRequestSummaryDto>>> GetRequestsAsync(
        [FromQuery] AdminCancellationRequestQuery query,
        CancellationToken ct)
    {
        return Ok(await cancellationService.GetAdminRequestsAsync(query, ct));
    }

    [HttpGet("{requestId:guid}")]
    public async Task<ActionResult<AdminCancellationRequestDetailDto>> GetRequestDetailAsync(
        Guid requestId,
        CancellationToken ct)
    {
        return Ok(await cancellationService.GetAdminRequestDetailAsync(requestId, ct));
    }

    [HttpPost("{requestId:guid}/decision")]
    public async Task<ActionResult<CancellationDecisionResponse>> ProcessDecisionAsync(
        Guid requestId,
        [FromBody] CancellationDecisionRequest request,
        CancellationToken ct)
    {
        return Ok(await cancellationService.ProcessDecisionAsync(AdminUserId, requestId, request, ct));
    }
}
