using CourtGo.Application.Cancellation;
using CourtGo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/admin/cancellation-policies")]
[Authorize(Roles = "Admin")]
public class AdminCancellationPoliciesController(IAdminCancellationPolicyService policyService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminCancellationPolicySummaryDto>>> GetPoliciesAsync(CancellationToken ct)
    {
        return Ok(await policyService.GetPoliciesAsync(ct));
    }

    [HttpGet("{policyId:guid}")]
    public async Task<ActionResult<AdminCancellationPolicyDetailDto>> GetPolicyByIdAsync(
        Guid policyId,
        CancellationToken ct)
    {
        return Ok(await policyService.GetPolicyByIdAsync(policyId, ct));
    }

    [HttpPost]
    public async Task<ActionResult<AdminCancellationPolicyDetailDto>> CreatePolicyAsync(
        [FromBody] CreateCancellationPolicyRequest request,
        CancellationToken ct)
    {
        var result = await policyService.CreatePolicyAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("{policyId:guid}/activate")]
    public async Task<ActionResult<AdminCancellationPolicyDetailDto>> ActivatePolicyAsync(
        Guid policyId,
        CancellationToken ct)
    {
        return Ok(await policyService.ActivatePolicyAsync(policyId, ct));
    }
}
