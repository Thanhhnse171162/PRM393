using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CourtGo.IntegrationTests;

public class DepositPaymentApiTests : IDisposable
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Factory(string environment = "Development") : BookingHoldApiTests.Factory
    {
        public Clock Clock { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment(environment);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Clock);
            });
        }
    }
    private readonly Factory _factory = new();
    private readonly HttpClient _client;
    public DepositPaymentApiTests() => _client = _factory.CreateClient();

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body != null) request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }
    private async Task<BookingHoldResponse> HoldAsync()
    {
        var day = DateOnly.FromDateTime(_factory.Clock.Now.AddDays(2).DateTime);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(10, 0)), TimeSpan.FromHours(7));
        var response = await SendAsync(HttpMethod.Post, "/api/bookings/hold", _factory.ActiveCustomerToken,
            new { courtId = _factory.ActiveCourtId, slotStartAts = new[] { start, start.AddHours(1) } });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookingHoldResponse>())!;
    }
    private async Task<DepositPaymentResponse> StartAsync(Guid id)
    {
        var response = await SendAsync(HttpMethod.Post, $"/api/bookings/{id}/payments/deposit",
            _factory.ActiveCustomerToken, new { paymentMethod = "VNPay" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DepositPaymentResponse>())!;
    }
    private static async Task ErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("start", "none")]
    [InlineData("start", "Staff")]
    [InlineData("start", "Admin")]
    [InlineData("status", "none")]
    [InlineData("status", "Staff")]
    [InlineData("status", "Admin")]
    [InlineData("simulate", "none")]
    [InlineData("simulate", "Staff")]
    [InlineData("simulate", "Admin")]
    public async Task PaymentEndpoints_RequireCustomer(string operation, string role)
    {
        var token = role switch { "Staff" => _factory.StaffToken, "Admin" => _factory.AdminToken, _ => null };
        var path = operation switch
        {
            "start" => $"/api/bookings/{Guid.NewGuid()}/payments/deposit",
            "status" => $"/api/bookings/{Guid.NewGuid()}/payment-status",
            _ => $"/api/dev/payments/{Guid.NewGuid()}/simulate"
        };
        var response = await SendAsync(operation == "status" ? HttpMethod.Get : HttpMethod.Post, path, token, new { });
        Assert.Equal(role == "none" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NonOwnerCannotStartPollOrSimulate()
    {
        var hold = await HoldAsync();
        var payment = await StartAsync(hold.BookingId);
        foreach (var id in new[] { hold.BookingId, Guid.NewGuid() })
        {
            await ErrorAsync(await SendAsync(HttpMethod.Post, $"/api/bookings/{id}/payments/deposit",
                _factory.SecondCustomerToken, new { paymentMethod = "MoMo" }), HttpStatusCode.NotFound, ErrorCodes.BookingNotFound);
            await ErrorAsync(await SendAsync(HttpMethod.Get, $"/api/bookings/{id}/payment-status",
                _factory.SecondCustomerToken), HttpStatusCode.NotFound, ErrorCodes.BookingNotFound);
        }
        await ErrorAsync(await SendAsync(HttpMethod.Post, $"/api/dev/payments/{payment.PaymentId}/simulate",
            _factory.SecondCustomerToken, new { result = "success" }), HttpStatusCode.NotFound, ErrorCodes.PaymentNotFound);
    }

    [Theory]
    [InlineData("amount", 1)]
    [InlineData("depositAmount", 1)]
    [InlineData("depositPercent", 100)]
    [InlineData("totalAmount", 1)]
    [InlineData("customerId", "00000000-0000-0000-0000-000000000001")]
    [InlineData("bookingStatus", "Confirmed")]
    public async Task ClientCannotOverrideMoneyIdentityOrStatus(string property, object value)
    {
        var hold = await HoldAsync();
        var payload = new Dictionary<string, object> { ["paymentMethod"] = "MoMo", [property] = value };
        await ErrorAsync(await SendAsync(HttpMethod.Post, $"/api/bookings/{hold.BookingId}/payments/deposit",
            _factory.ActiveCustomerToken, payload), HttpStatusCode.BadRequest, ErrorCodes.ValidationError);
        using var scope = _factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<CourtGoDbContext>().Payments.ToListAsync());
    }

    [Fact]
    public async Task ConcurrentStartsAndCallbacks_ProduceOnePaymentHistoryNotificationAndQr()
    {
        var hold = await HoldAsync();
        var attempts = await Task.WhenAll(StartAsync(hold.BookingId), StartAsync(hold.BookingId));
        Assert.Equal(attempts[0].PaymentId, attempts[1].PaymentId);
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => SendAsync(HttpMethod.Post,
            $"/api/dev/payments/{attempts[0].PaymentId}/simulate", _factory.ActiveCustomerToken, new { result = "success" })));
        Assert.All(results, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var first = (await results[0].Content.ReadFromJsonAsync<DepositConfirmationResponse>())!;
        Assert.Equal(first, await results[1].Content.ReadFromJsonAsync<DepositConfirmationResponse>());
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var booking = await db.Bookings.AsNoTracking().SingleAsync();
        Assert.Equal(BookingStatus.Confirmed, booking.BookingStatus);
        Assert.Single(await db.Payments.ToListAsync());
        Assert.Single(await db.BookingStatusHistories.Where(h => h.ToStatus == BookingStatus.Confirmed).ToListAsync());
        Assert.Single(await db.Notifications.ToListAsync());
        Assert.DoesNotContain(booking.QrToken!, JsonSerializer.Serialize(first));
        var response = await SendAsync(HttpMethod.Get, $"/api/bookings/{hold.BookingId}/payment-status", _factory.ActiveCustomerToken);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("providerTransactionId", json);
        Assert.DoesNotContain(booking.QrToken!, json);
        var status = JsonSerializer.Deserialize<BookingPaymentStatusResponse>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(60000, status.PaidAmount);
        Assert.Equal(140000, status.RemainingAmount);
        Assert.Equal("Succeeded", Assert.Single(status.Payments).TransactionStatus);
    }

    [Theory]
    [InlineData("failed")]
    [InlineData("cancelled")]
    public async Task FailedOrCancelledDeposit_CanRetryThroughApis(string result)
    {
        var hold = await HoldAsync();
        var payment = await StartAsync(hold.BookingId);
        var response = await SendAsync(HttpMethod.Post, $"/api/dev/payments/{payment.PaymentId}/simulate",
            _factory.ActiveCustomerToken, new { result });
        response.EnsureSuccessStatusCode();
        Assert.Equal("PendingPayment", (await response.Content.ReadFromJsonAsync<DepositConfirmationResponse>())!.BookingStatus);
        await ErrorAsync(await SendAsync(HttpMethod.Get, $"/api/bookings/{hold.BookingId}/qr",
            _factory.ActiveCustomerToken), HttpStatusCode.Conflict, ErrorCodes.QrNotAvailable);
        var retry = await StartAsync(hold.BookingId);
        Assert.NotEqual(payment.PaymentId, retry.PaymentId);
        (await SendAsync(HttpMethod.Post, $"/api/dev/payments/{retry.PaymentId}/simulate",
            _factory.ActiveCustomerToken, new { result = "success" })).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ExpiredHoldAndLateCallback_AreRejected_WithoutRevivingSlots()
    {
        var hold = await HoldAsync();
        var payment = await StartAsync(hold.BookingId);
        _factory.Clock.Now = hold.HoldExpiresAt.AddSeconds(1);
        await ErrorAsync(await SendAsync(HttpMethod.Post, $"/api/dev/payments/{payment.PaymentId}/simulate",
            _factory.ActiveCustomerToken, new { result = "success" }), HttpStatusCode.Conflict, ErrorCodes.PaymentAfterHoldExpired);
        await ErrorAsync(await SendAsync(HttpMethod.Post, $"/api/bookings/{hold.BookingId}/payments/deposit",
            _factory.ActiveCustomerToken, new { paymentMethod = "MoMo" }), HttpStatusCode.Conflict, ErrorCodes.BookingHoldExpired);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        Assert.Equal(BookingStatus.Expired, (await db.Bookings.SingleAsync()).BookingStatus);
        Assert.All(await db.BookingSlots.ToListAsync(), s => Assert.False(s.IsOccupying));
        Assert.Equal(PaymentTransactionStatus.Pending, (await db.Payments.SingleAsync()).TransactionStatus);
        Assert.Single(await db.BookingStatusHistories.Where(h => h.ToStatus == BookingStatus.Expired).ToListAsync());
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task SimulationRouteAndGateway_AreAbsentOutsideDevelopment(string environment)
    {
        using var factory = new Factory(environment);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/dev/payments/{Guid.NewGuid()}/simulate")
        { Content = JsonContent.Create(new { result = "success" }) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", factory.ActiveCustomerToken);
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(request)).StatusCode);
        Assert.Null(factory.Services.GetService<IDevelopmentPaymentGateway>());
        Assert.Null(factory.Services.GetService<IDevelopmentPaymentSimulationService>());
        Assert.Equal("Unavailable", factory.Services.GetRequiredService<IPaymentGateway>().Name);
    }

    [Fact]
    public async Task SwaggerContainsExactPaymentRoutes()
    {
        var document = await _client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var paths = document.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/bookings/{bookingId}/payments/deposit").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/api/bookings/{bookingId}/payment-status").TryGetProperty("get", out _));
        Assert.True(paths.GetProperty("/api/dev/payments/{paymentId}/simulate").TryGetProperty("post", out _));
    }

    public void Dispose() { _client.Dispose(); _factory.Dispose(); }
}

