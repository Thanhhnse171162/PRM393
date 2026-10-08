using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CourtGo.Application.Bookings;
using CourtGo.Application.Notifications;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CourtGo.IntegrationTests;

public class NotificationsApiTests : IDisposable
{
    private readonly BookingHoldApiTests.Factory _factory = new();
    private readonly HttpClient _client;

    public NotificationsApiTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task SeedNotificationsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();

        // Customer 1 notifications: 2 unread, 1 read
        db.Notifications.AddRange(
            new Notification
            {
                UserId = _factory.ActiveCustomer.Id,
                Type = NotificationType.Booking,
                Title = "Lịch 1",
                Message = "Thông báo 1",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
            },
            new Notification
            {
                UserId = _factory.ActiveCustomer.Id,
                Type = NotificationType.Payment,
                Title = "Hoàn tiền 2",
                Message = "Thông báo 2",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
            },
            new Notification
            {
                UserId = _factory.ActiveCustomer.Id,
                Type = NotificationType.System,
                Title = "Hệ thống 3",
                Message = "Thông báo 3",
                IsRead = true,
                ReadAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-20)
            });

        // Customer 2 notification: 1 unread
        db.Notifications.Add(new Notification
        {
            UserId = _factory.SecondCustomer.Id,
            Type = NotificationType.Booking,
            Title = "Lịch người khác",
            Message = "Nội dung bí mật",
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Notifications_RequireAuthentication()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var listRes = await _client.GetAsync("/api/notifications");
        var countRes = await _client.GetAsync("/api/notifications/unread-count");
        var readOneRes = await _client.PatchAsync($"/api/notifications/{Guid.NewGuid()}/read", null);
        var readAllRes = await _client.PatchAsync("/api/notifications/read-all", null);

        Assert.Equal(HttpStatusCode.Unauthorized, listRes.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, countRes.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, readOneRes.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, readAllRes.StatusCode);
    }

    [Fact]
    public async Task Notifications_ListAndUnreadCount_IsolateToCaller()
    {
        await SeedNotificationsAsync();

        // Customer 1 list
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        var listRes = await _client.GetFromJsonAsync<PagedResult<NotificationDto>>("/api/notifications");
        Assert.NotNull(listRes);
        Assert.Equal(3, listRes.TotalItems);
        Assert.DoesNotContain(listRes.Items, n => n.Title == "Lịch người khác");

        // Customer 1 unread count
        var countRes = await _client.GetFromJsonAsync<UnreadNotificationCountDto>("/api/notifications/unread-count");
        Assert.NotNull(countRes);
        Assert.Equal(2, countRes.UnreadCount);

        // Filter by isRead = false
        var unreadOnly = await _client.GetFromJsonAsync<PagedResult<NotificationDto>>("/api/notifications?isRead=false");
        Assert.NotNull(unreadOnly);
        Assert.Equal(2, unreadOnly.TotalItems);

        // Filter by isRead = true
        var readOnly = await _client.GetFromJsonAsync<PagedResult<NotificationDto>>("/api/notifications?isRead=true");
        Assert.NotNull(readOnly);
        Assert.Equal(1, readOnly.TotalItems);

        // Customer 2 list & count
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.SecondCustomerToken);
        var c2List = await _client.GetFromJsonAsync<PagedResult<NotificationDto>>("/api/notifications");
        Assert.NotNull(c2List);
        Assert.Equal(1, c2List.TotalItems);
        Assert.Equal("Lịch người khác", c2List.Items[0].Title);

        var c2Count = await _client.GetFromJsonAsync<UnreadNotificationCountDto>("/api/notifications/unread-count");
        Assert.NotNull(c2Count);
        Assert.Equal(1, c2Count.UnreadCount);
    }

    [Fact]
    public async Task Notifications_MarkOneAsRead_OnlyModifiesOwnerAndIsIdempotent()
    {
        await SeedNotificationsAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var c1Notification = await db.Notifications.FirstAsync(n => n.UserId == _factory.ActiveCustomer.Id && !n.IsRead);
        var c2Notification = await db.Notifications.FirstAsync(n => n.UserId == _factory.SecondCustomer.Id);

        // Customer 2 cannot mark Customer 1's notification -> 404
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.SecondCustomerToken);
        var foreignRes = await _client.PatchAsync($"/api/notifications/{c1Notification.Id}/read", null);
        Assert.Equal(HttpStatusCode.NotFound, foreignRes.StatusCode);

        // Customer 1 marks own notification
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        var successRes = await _client.PatchAsync($"/api/notifications/{c1Notification.Id}/read", null);
        Assert.Equal(HttpStatusCode.OK, successRes.StatusCode);
        var dto = await successRes.Content.ReadFromJsonAsync<NotificationDto>();
        Assert.NotNull(dto);
        Assert.True(dto.IsRead);
        Assert.NotNull(dto.ReadAt);

        // Idempotent call
        var repeatedRes = await _client.PatchAsync($"/api/notifications/{c1Notification.Id}/read", null);
        Assert.Equal(HttpStatusCode.OK, repeatedRes.StatusCode);
        var repeatedDto = await repeatedRes.Content.ReadFromJsonAsync<NotificationDto>();
        Assert.True(repeatedDto!.IsRead);
    }

    [Fact]
    public async Task Notifications_MarkAllAsRead_OnlyUpdatesCallerUnread()
    {
        await SeedNotificationsAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.ActiveCustomerToken);
        var readAllRes = await _client.PatchAsync("/api/notifications/read-all", null);
        Assert.Equal(HttpStatusCode.OK, readAllRes.StatusCode);

        // Check Customer 1 unread count is now 0
        var count = await _client.GetFromJsonAsync<UnreadNotificationCountDto>("/api/notifications/unread-count");
        Assert.NotNull(count);
        Assert.Equal(0, count.UnreadCount);

        // Check Customer 2 still has unread notification
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.SecondCustomerToken);
        var c2Count = await _client.GetFromJsonAsync<UnreadNotificationCountDto>("/api/notifications/unread-count");
        Assert.NotNull(c2Count);
        Assert.Equal(1, c2Count.UnreadCount);
    }
}
