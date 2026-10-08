using CourtGo.Application.AdminDashboard;
using CourtGo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController(IAdminDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminDashboardResponse>> GetDashboardAsync(
        [FromQuery] AdminDashboardQuery query,
        CancellationToken ct)
    {
        return Ok(await dashboardService.GetDashboardAsync(query, ct));
    }
}
