using CourtGo.Application.Bookings;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/admin/staff")]
[Authorize(Roles = "Admin")]
public class AdminStaffController(IAdminStaffService staffService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminStaffSummaryDto>>> GetStaffListAsync(
        [FromQuery] AdminStaffQuery query,
        CancellationToken ct)
    {
        return Ok(await staffService.GetStaffListAsync(query, ct));
    }

    [HttpGet("{staffId:guid}")]
    public async Task<ActionResult<AdminStaffDetailDto>> GetStaffDetailAsync(
        Guid staffId,
        CancellationToken ct)
    {
        return Ok(await staffService.GetStaffDetailAsync(staffId, ct));
    }

    [HttpPost]
    public async Task<ActionResult<AdminStaffDetailDto>> CreateStaffAsync(
        [FromBody] CreateStaffRequest request,
        CancellationToken ct)
    {
        return Ok(await staffService.CreateStaffAsync(request, ct));
    }

    [HttpPut("{staffId:guid}")]
    public async Task<ActionResult<AdminStaffDetailDto>> UpdateStaffProfileAsync(
        Guid staffId,
        [FromBody] UpdateStaffProfileRequest request,
        CancellationToken ct)
    {
        return Ok(await staffService.UpdateStaffProfileAsync(staffId, request, ct));
    }

    [HttpPatch("{staffId:guid}/status")]
    public async Task<ActionResult<AdminStaffDetailDto>> UpdateStaffStatusAsync(
        Guid staffId,
        [FromBody] UpdateStaffStatusRequest request,
        CancellationToken ct)
    {
        return Ok(await staffService.UpdateStaffStatusAsync(staffId, request, ct));
    }

    [HttpPut("{staffId:guid}/assignment")]
    public async Task<ActionResult<AdminStaffDetailDto>> ReassignStaffAsync(
        Guid staffId,
        [FromBody] ReassignStaffRequest request,
        CancellationToken ct)
    {
        return Ok(await staffService.ReassignStaffAsync(staffId, request, ct));
    }
}
