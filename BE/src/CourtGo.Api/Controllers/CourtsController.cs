using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/courts")]
[AllowAnonymous]
public class CourtsController : ControllerBase
{
    /// <summary>Placeholder endpoint so the route is visible in Swagger.</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { module = "courts", status = "not-implemented" });
}
