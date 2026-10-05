using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    /// <summary>Placeholder endpoint so the route is visible in Swagger.</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { module = "admin", status = "not-implemented" });
}
