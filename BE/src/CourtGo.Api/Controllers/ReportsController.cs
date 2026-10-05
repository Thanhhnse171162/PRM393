using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    /// <summary>Placeholder endpoint so the route is visible in Swagger.</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { module = "reports", status = "not-implemented" });
}
