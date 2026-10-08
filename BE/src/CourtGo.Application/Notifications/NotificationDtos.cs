namespace CourtGo.Application.Notifications;

public record NotificationDto(
    Guid NotificationId,
    string Type,
    string Title,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt,
    string? ReferenceType,
    Guid? ReferenceId,
    string? DeepLink);

public record UnreadNotificationCountDto(int UnreadCount)
{
    public int Count => UnreadCount;
}
