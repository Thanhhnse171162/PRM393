using System.Security.Claims;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/admin/system-settings")]
[Authorize(Roles = "Admin")]
public class AdminSystemSettingsController(IAdminSystemSettingService settingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SystemSettingsDto>> GetSettingsAsync(CancellationToken ct)
    {
        return Ok(await settingService.GetSettingsAsync(ct));
    }

    [HttpPut]
    public async Task<ActionResult<SystemSettingsDto>> UpdateSettingsAsync(
        [FromBody] UpdateSystemSettingsRequest request,
        CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;

        return Ok(await settingService.UpdateSettingsAsync(userId, request, ct));
    }
}
