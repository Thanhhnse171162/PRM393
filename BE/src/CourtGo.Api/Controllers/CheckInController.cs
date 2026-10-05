using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/check-in")]
[Authorize(Roles = "Staff")]
public class CheckInController : ControllerBase
{
    /// <summary>Placeholder endpoint so the route is visible in Swagger.</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { module = "check-in", status = "not-implemented" });
}
