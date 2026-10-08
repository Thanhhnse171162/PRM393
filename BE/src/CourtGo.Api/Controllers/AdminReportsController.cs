using CourtGo.Application.AdminReports;
using CourtGo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public class AdminReportsController(IAdminReportService reportService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<AdminReportSummaryResponse>> GetSummaryAsync(
        [FromQuery] AdminReportSummaryQuery query,
        CancellationToken ct)
    {
        return Ok(await reportService.GetSummaryReportAsync(query, ct));
    }

    [HttpGet("revenue")]
    public async Task<ActionResult<AdminRevenueReportResponse>> GetRevenueAsync(
        [FromQuery] AdminRevenueReportQuery query,
        CancellationToken ct)
    {
        return Ok(await reportService.GetRevenueReportAsync(query, ct));
    }

    [HttpGet("bookings")]
    public async Task<ActionResult<AdminBookingReportResponse>> GetBookingsAsync(
        [FromQuery] AdminBookingReportQuery query,
        CancellationToken ct)
    {
        return Ok(await reportService.GetBookingReportAsync(query, ct));
    }

    [HttpGet("occupancy")]
    public async Task<ActionResult<AdminOccupancyReportResponse>> GetOccupancyAsync(
        [FromQuery] AdminOccupancyReportQuery query,
        CancellationToken ct)
    {
        return Ok(await reportService.GetOccupancyReportAsync(query, ct));
    }
}
