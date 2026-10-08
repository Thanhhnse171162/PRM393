using CourtGo.Application.Bookings;
using CourtGo.Application.Common;
using CourtGo.Application.Notifications;

namespace CourtGo.Application.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, bool? isRead, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<UnreadNotificationCountDto> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<NotificationDto> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
}
