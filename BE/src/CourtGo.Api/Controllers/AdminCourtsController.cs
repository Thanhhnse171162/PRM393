using CourtGo.Application.Bookings;
using CourtGo.Application.Courts;
using CourtGo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/admin/courts")]
[Authorize(Roles = "Admin")]
public class AdminCourtsController(IAdminCourtService courtService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminCourtListItemDto>>> GetCourtsAsync(
        [FromQuery] AdminCourtQuery query,
        CancellationToken ct)
    {
        return Ok(await courtService.GetCourtsAsync(query, ct));
    }

    [HttpGet("{courtId:guid}")]
    public async Task<ActionResult<AdminCourtDetailDto>> GetCourtByIdAsync(
        Guid courtId,
        CancellationToken ct)
    {
        return Ok(await courtService.GetCourtByIdAsync(courtId, ct));
    }

    [HttpPost]
    public async Task<ActionResult<AdminCourtDetailDto>> CreateCourtAsync(
        [FromBody] CreateCourtRequest request,
        CancellationToken ct)
    {
        return Ok(await courtService.CreateCourtAsync(request, ct));
    }

    [HttpPut("{courtId:guid}")]
    public async Task<ActionResult<AdminCourtDetailDto>> UpdateCourtAsync(
        Guid courtId,
        [FromBody] UpdateCourtRequest request,
        CancellationToken ct)
    {
        return Ok(await courtService.UpdateCourtAsync(courtId, request, ct));
    }

    [HttpPatch("{courtId:guid}/status")]
    public async Task<ActionResult<AdminCourtDetailDto>> UpdateCourtStatusAsync(
        Guid courtId,
        [FromBody] UpdateCourtStatusRequest request,
        CancellationToken ct)
    {
        return Ok(await courtService.UpdateCourtStatusAsync(courtId, request, ct));
    }
}
