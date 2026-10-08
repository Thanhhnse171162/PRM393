using CourtGo.Application.Bookings;
using CourtGo.Application.Interfaces;
using CourtGo.Application.SportCenters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/admin/sport-centers")]
[Authorize(Roles = "Admin")]
public class AdminSportCentersController(IAdminSportCenterService centerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminSportCenterListItemDto>>> GetSportCentersAsync(
        [FromQuery] AdminSportCenterQuery query,
        CancellationToken ct)
    {
        return Ok(await centerService.GetSportCentersAsync(query, ct));
    }

    [HttpGet("{centerId:guid}")]
    public async Task<ActionResult<AdminSportCenterDetailDto>> GetSportCenterByIdAsync(
        Guid centerId,
        CancellationToken ct)
    {
        return Ok(await centerService.GetSportCenterByIdAsync(centerId, ct));
    }

    [HttpPost]
    public async Task<ActionResult<AdminSportCenterDetailDto>> CreateSportCenterAsync(
        [FromBody] CreateSportCenterRequest request,
        CancellationToken ct)
    {
        return Ok(await centerService.CreateSportCenterAsync(request, ct));
    }

    [HttpPut("{centerId:guid}")]
    public async Task<ActionResult<AdminSportCenterDetailDto>> UpdateSportCenterAsync(
        Guid centerId,
        [FromBody] UpdateSportCenterRequest request,
        CancellationToken ct)
    {
        return Ok(await centerService.UpdateSportCenterAsync(centerId, request, ct));
    }

    [HttpPatch("{centerId:guid}/status")]
    public async Task<ActionResult<AdminSportCenterDetailDto>> UpdateSportCenterStatusAsync(
        Guid centerId,
        [FromBody] UpdateSportCenterStatusRequest request,
        CancellationToken ct)
    {
        return Ok(await centerService.UpdateSportCenterStatusAsync(centerId, request, ct));
    }
}
