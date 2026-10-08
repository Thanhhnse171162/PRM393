using System.Security.Claims;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtGo.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetNotificationsAsync(
        [FromQuery] bool? isRead,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Ok(await notificationService.GetNotificationsAsync(UserId, isRead, pageNumber, pageSize, ct));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadNotificationCountDto>> GetUnreadCountAsync(CancellationToken ct = default)
    {
        return Ok(await notificationService.GetUnreadCountAsync(UserId, ct));
    }

    [HttpPatch("{notificationId:guid}/read")]
    public async Task<ActionResult<NotificationDto>> MarkAsReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        return Ok(await notificationService.MarkAsReadAsync(UserId, notificationId, ct));
    }

    [HttpPatch("read-all")]
    public async Task<ActionResult<object>> MarkAllAsReadAsync(CancellationToken ct = default)
    {
        var updatedCount = await notificationService.MarkAllAsReadAsync(UserId, ct);
        return Ok(new { updatedCount });
    }
}
