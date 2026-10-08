using CourtGo.Application.Bookings;
using CourtGo.Application.Common;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Notifications;
using CourtGo.Domain.Entities;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public class NotificationService(CourtGoDbContext db, TimeProvider clock) : INotificationService
{
    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, bool? isRead, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        StaffOperationsService.ValidatePage(pageNumber, pageSize);
        var query = db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type.ToString(),
                n.Title,
                n.Message,
                n.IsRead,
                n.CreatedAt,
                n.ReadAt,
                n.ReferenceType,
                n.ReferenceId,
                n.DeepLink))
            .ToListAsync(ct);

        return new(items, pageNumber, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<UnreadNotificationCountDto> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        var count = await db.Notifications.AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
        return new(count);
    }

    public async Task<NotificationDto> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var notification = await db.Notifications.SingleOrDefaultAsync(n => n.Id == notificationId, ct);
        if (notification is null || notification.UserId != userId)
        {
            throw new NotFoundException("Notification not found.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = clock.GetUtcNow();
            await db.SaveChangesAsync(ct);
        }

        return new NotificationDto(
            notification.Id,
            notification.Type.ToString(),
            notification.Title,
            notification.Message,
            notification.IsRead,
            notification.CreatedAt,
            notification.ReadAt,
            notification.ReferenceType,
            notification.ReferenceId,
            notification.DeepLink);
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        if (db.Database.IsRelational())
        {
            return await db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, now), ct);
        }

        var unread = await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var item in unread)
        {
            item.IsRead = true;
            item.ReadAt = now;
        }

        await db.SaveChangesAsync(ct);
        return unread.Count;
    }
}
