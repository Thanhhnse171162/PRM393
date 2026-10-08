using CourtGo.Application.Bookings;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Sports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/admin/sports")]
[Authorize(Roles = "Admin")]
public class AdminSportsController(IAdminSportService sportService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminSportDto>>> GetSportsAsync(
        [FromQuery] AdminSportQuery query,
        CancellationToken ct)
    {
        return Ok(await sportService.GetSportsAsync(query, ct));
    }

    [HttpGet("{sportId:guid}")]
    public async Task<ActionResult<AdminSportDetailDto>> GetSportByIdAsync(
        Guid sportId,
        CancellationToken ct)
    {
        return Ok(await sportService.GetSportByIdAsync(sportId, ct));
    }

    [HttpPost]
    public async Task<ActionResult<AdminSportDetailDto>> CreateSportAsync(
        [FromBody] CreateSportRequest request,
        CancellationToken ct)
    {
        return Ok(await sportService.CreateSportAsync(request, ct));
    }

    [HttpPut("{sportId:guid}")]
    public async Task<ActionResult<AdminSportDetailDto>> UpdateSportAsync(
        Guid sportId,
        [FromBody] UpdateSportRequest request,
        CancellationToken ct)
    {
        return Ok(await sportService.UpdateSportAsync(sportId, request, ct));
    }

    [HttpPatch("{sportId:guid}/status")]
    public async Task<ActionResult<AdminSportDetailDto>> UpdateSportStatusAsync(
        Guid sportId,
        [FromBody] UpdateSportStatusRequest request,
        CancellationToken ct)
    {
        return Ok(await sportService.UpdateSportStatusAsync(sportId, request, ct));
    }
}
