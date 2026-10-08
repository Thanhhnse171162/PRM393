using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CourtGo.Application.Auth;
using CourtGo.Application.Availability;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CourtGo.IntegrationTests;

public class BookingWorkflowApiTests : IDisposable
{
    private readonly BookingHoldApiTests.Factory _factory = new();
    private readonly HttpClient _client;
    public BookingWorkflowApiTests() => _client = _factory.CreateClient();

    private async Task<Booking> SeedBookingAsync(bool paid = false, BookingStatus status = BookingStatus.Confirmed, bool assigned = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var booking = new Booking
        {
            BookingCode = "TEST-" + Guid.NewGuid().ToString("N")[..20],
            CustomerUserId = _factory.ActiveCustomer.Id, CreatedByUserId = _factory.ActiveCustomer.Id,
            CourtId = _factory.ActiveCourtId, CourtNameSnapshot = "Snapshot court", CenterNameSnapshot = "Snapshot center",
            SportNameSnapshot = "Snapshot sport", CustomerNameSnapshot = "Snapshot customer", CustomerPhoneSnapshot = "0901111111",
            StartAt = DateTimeOffset.UtcNow.AddDays(2), EndAt = DateTimeOffset.UtcNow.AddDays(2).AddHours(2),
            DurationMinutes = 120, TotalAmount = 200000, DepositAmount = 60000, DepositPercentSnapshot = 30,
            BookingStatus = status, PaymentStatus = paid ? BookingPaymentStatus.FullyPaid : BookingPaymentStatus.DepositPaid,
            QrToken = "Test-Secret-" + Guid.NewGuid().ToString("N")
        };
        booking.Payments.Add(new Payment { PaymentKind = PaymentKind.Deposit, PaymentMethod = PaymentMethod.BankTransfer,
            Amount = 60000, TransactionStatus = PaymentTransactionStatus.Succeeded, PaidAt = DateTimeOffset.UtcNow });
        if (paid)
            booking.Payments.Add(new Payment { PaymentKind = PaymentKind.Remaining, PaymentMethod = PaymentMethod.Cash,
                Amount = 140000, TransactionStatus = PaymentTransactionStatus.Succeeded, PaidAt = DateTimeOffset.UtcNow });
        db.Bookings.Add(booking);
        if (assigned)
            db.StaffAssignments.Add(new StaffAssignment { StaffUserId = _factory.StaffUser.Id, SportCenterId = _factory.CenterId });
        await db.SaveChangesAsync();
        return booking;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, body.GetProperty("code").GetString());
    }

    public static IEnumerable<object[]> ProtectedRoutes()
    {
        foreach (var route in new[] { "check-ins/verify", "bookings/{id}/payments/remaining", "bookings/{id}/check-ins" })
            foreach (var role in new[] { "none", "Customer", "Admin" })
                yield return new object[] { route, role };
    }

    [Theory]
    [MemberData(nameof(ProtectedRoutes))]
    public async Task StaffEndpoints_EnforceRoles(string path, string role)
    {
        var token = role switch { "Customer" => _factory.ActiveCustomerToken, "Admin" => _factory.AdminToken, _ => null };
        var response = await SendAsync(HttpMethod.Post, "/api/staff/" + path.Replace("{id}", Guid.NewGuid().ToString()),
            token, new { qrToken = "anything", paymentMethod = "Cash" });
        Assert.Equal(role == "none" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/{id}")]
    [InlineData("/{id}/qr")]
    public async Task CustomerEndpoints_EnforceRolesAndAuthentication(string path)
    {
        var url = "/api/bookings" + path.Replace("{id}", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, url, null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, url, _factory.StaffToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, url, _factory.AdminToken)).StatusCode);
    }

    [Fact]
    public async Task CustomerErrors_UseSecure404_AndInvalidGroup400()
    {
        var booking = await SeedBookingAsync();
        foreach (var id in new[] { booking.Id, Guid.NewGuid() })
            foreach (var suffix in new[] { "", "/qr" })
                await AssertErrorAsync(await SendAsync(HttpMethod.Get, $"/api/bookings/{id}{suffix}", _factory.SecondCustomerToken),
                    HttpStatusCode.NotFound, ErrorCodes.BookingNotFound);
        await AssertErrorAsync(await SendAsync(HttpMethod.Get, "/api/bookings?statusGroup=wrong", _factory.ActiveCustomerToken),
            HttpStatusCode.BadRequest, ErrorCodes.InvalidBookingStatusGroup);
    }

    [Fact]
    public async Task Verify_IsReadOnly_UsesActualPayments_AndRejectsInvalidQr()
    {
        var booking = await SeedBookingAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        db.Payments.AddRange(
            new Payment { BookingId = booking.Id, PaymentKind = PaymentKind.Refund, Amount = 90000, TransactionStatus = PaymentTransactionStatus.Succeeded },
            new Payment { BookingId = booking.Id, PaymentKind = PaymentKind.Remaining, Amount = 90000, TransactionStatus = PaymentTransactionStatus.Pending });
        await db.SaveChangesAsync();
        var response = await SendAsync(HttpMethod.Post, "/api/staff/check-ins/verify", _factory.StaffToken, new { booking.QrToken });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<StaffQrVerificationResponse>();
        Assert.Equal(140000, result!.Payment.RemainingAmount);
        Assert.True(result.RequiresRemainingPayment);
        Assert.False(result.AlreadyCheckedIn);
        Assert.Equal(BookingStatus.Confirmed, (await db.Bookings.AsNoTracking().SingleAsync()).BookingStatus);
        Assert.Equal(3, await db.Payments.CountAsync());
        Assert.Empty(await db.CheckIns.ToListAsync());
        Assert.Empty(await db.BookingStatusHistories.ToListAsync());
        Assert.Empty(await db.Notifications.ToListAsync());
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, "/api/staff/check-ins/verify", _factory.StaffToken,
            new { qrToken = "invalid" }), HttpStatusCode.NotFound, ErrorCodes.QrInvalid);
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, "/api/staff/check-ins/verify", _factory.StaffToken,
            new { qrToken = booking.QrToken!.ToUpperInvariant() }), HttpStatusCode.NotFound, ErrorCodes.QrInvalid);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EveryStaffOperation_RevalidatesCenterAssignment(bool wrongCenter)
    {
        var booking = await SeedBookingAsync(assigned: false);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        if (wrongCenter)
        {
            db.StaffAssignments.Add(new StaffAssignment { StaffUserId = _factory.StaffUser.Id, SportCenterId = Guid.NewGuid() });
            await db.SaveChangesAsync();
        }
        var expected = wrongCenter ? ErrorCodes.StaffCenterAccessDenied : ErrorCodes.StaffNotAssigned;
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, "/api/staff/check-ins/verify", _factory.StaffToken, new { booking.QrToken }),
            HttpStatusCode.Forbidden, expected);
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{booking.Id}/payments/remaining",
            _factory.StaffToken, new { paymentMethod = "Cash" }), HttpStatusCode.Forbidden, expected);
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{booking.Id}/check-ins",
            _factory.StaffToken, new { booking.QrToken }), HttpStatusCode.Forbidden, expected);
        Assert.Single(await db.Payments.ToListAsync());
        Assert.Empty(await db.CheckIns.ToListAsync());
    }

    [Theory]
    [InlineData(BookingStatus.PendingPayment)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Expired)]
    [InlineData(BookingStatus.NoShow)]
    [InlineData(BookingStatus.Completed)]
    public async Task IneligibleStates_CannotVerifyCollectOrCheckIn(BookingStatus status)
    {
        var booking = await SeedBookingAsync(status: status);
        foreach (var operation in new[] { "verify", "remaining", "check-in" })
        {
            var path = operation switch
            {
                "verify" => "/api/staff/check-ins/verify",
                "remaining" => $"/api/staff/bookings/{booking.Id}/payments/remaining",
                _ => $"/api/staff/bookings/{booking.Id}/check-ins"
            };
            object body = operation == "remaining" ? new { paymentMethod = "Cash" } : new { booking.QrToken };
            await AssertErrorAsync(await SendAsync(HttpMethod.Post, path, _factory.StaffToken, body),
                HttpStatusCode.Conflict, ErrorCodes.BookingNotCheckInEligible);
        }
    }

    [Theory]
    [InlineData("Cash")]
    [InlineData("BankTransfer")]
    public async Task RemainingPayment_ComputesAmountAndAudit_AndRejectsDuplicate(string method)
    {
        var booking = await SeedBookingAsync();
        var path = $"/api/staff/bookings/{booking.Id}/payments/remaining";
        var response = await SendAsync(HttpMethod.Post, path, _factory.StaffToken, new { paymentMethod = method });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CollectRemainingPaymentResponse>();
        Assert.Equal(140000, result!.AmountCollected);
        Assert.Equal(200000, result.PaidAmount);
        Assert.Equal(0, result.RemainingAmount);
        Assert.Equal("FullyPaid", result.PaymentStatus);
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, path, _factory.StaffToken, new { paymentMethod = method }),
            HttpStatusCode.Conflict, ErrorCodes.PaymentAlreadyFullyPaid);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var payment = await db.Payments.SingleAsync(p => p.PaymentKind == PaymentKind.Remaining);
        Assert.Equal(_factory.StaffUser.Id, payment.ConfirmedByUserId);
        Assert.Equal(_factory.ActiveCustomer.Id, payment.PaidByUserId);
        Assert.Equal(PaymentTransactionStatus.Succeeded, payment.TransactionStatus);
        Assert.Equal(Enum.Parse<PaymentMethod>(method), payment.PaymentMethod);
        Assert.Null(payment.ProviderTransactionId);
        Assert.NotNull(payment.PaidAt);
        Assert.Equal(BookingStatus.Confirmed, (await db.Bookings.SingleAsync()).BookingStatus);
    }

    [Theory]
    [InlineData("MoMo")]
    [InlineData("VNPay")]
    [InlineData("Other")]
    [InlineData("1")]
    [InlineData(null)]
    public async Task UnsupportedManualMethods_AreRejected(string? method)
    {
        var booking = await SeedBookingAsync();
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{booking.Id}/payments/remaining",
            _factory.StaffToken, new { paymentMethod = method }), HttpStatusCode.BadRequest, ErrorCodes.UnsupportedPaymentMethod);
    }

    [Fact]
    public async Task ClientCannotOverrideMoney_AndCheckInRevalidatesQr()
    {
        var booking = await SeedBookingAsync();
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{booking.Id}/payments/remaining",
            _factory.StaffToken, new { paymentMethod = "Cash", amount = 1 }), HttpStatusCode.BadRequest, ErrorCodes.ValidationError);
        await SendAsync(HttpMethod.Post, "/api/staff/check-ins/verify", _factory.StaffToken, new { booking.QrToken });
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{booking.Id}/check-ins",
            _factory.StaffToken, new { qrToken = "wrong" }), HttpStatusCode.BadRequest, ErrorCodes.QrInvalid);
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{booking.Id}/check-ins",
            _factory.StaffToken, new { booking.QrToken }), HttpStatusCode.Conflict, ErrorCodes.OutstandingPaymentRequired);
    }

    [Theory]
    [InlineData(false, true, "approved", false)]
    [InlineData(true, false, "approved", false)]
    [InlineData(true, true, null, false)]
    [InlineData(true, true, "   ", false)]
    [InlineData(true, true, "Manager approved", true)]
    public async Task OutstandingOverride_RequiresSettingRequestAndReason(bool setting, bool requested, string? reason, bool success)
    {
        var booking = await SeedBookingAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        (await db.SystemSettings.SingleAsync()).AllowOutstandingCheckIn = setting;
        await db.SaveChangesAsync();
        var response = await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{booking.Id}/check-ins",
            _factory.StaffToken, new { booking.QrToken, allowOutstandingPayment = requested, overrideReason = reason });
        if (!success)
        {
            await AssertErrorAsync(response, HttpStatusCode.Conflict, ErrorCodes.OutstandingPaymentRequired);
            Assert.Empty(await db.CheckIns.ToListAsync());
            return;
        }
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var checkIn = await db.CheckIns.SingleAsync();
        Assert.True(checkIn.OutstandingPaymentOverride);
        Assert.Equal(reason, checkIn.OverrideReason);
        Assert.Equal(BookingPaymentStatus.DepositPaid, (await db.Bookings.SingleAsync()).PaymentStatus);
        // Staff can collect the balance after an explicit outstanding-payment check-in.
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Post,
            $"/api/staff/bookings/{booking.Id}/payments/remaining", _factory.StaffToken, new { paymentMethod = "Cash" })).StatusCode);
    }

    [Fact]
    public async Task ConcurrentCollectionAndCheckIn_CreateOnePaymentAndOneCheckIn()
    {
        var booking = await SeedBookingAsync();
        var payments = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => SendAsync(HttpMethod.Post,
            $"/api/staff/bookings/{booking.Id}/payments/remaining", _factory.StaffToken, new { paymentMethod = "Cash" })));
        Assert.Single(payments.Where(r => r.StatusCode == HttpStatusCode.OK));
        await AssertErrorAsync(payments.Single(r => r.StatusCode != HttpStatusCode.OK),
            HttpStatusCode.Conflict, ErrorCodes.PaymentAlreadyFullyPaid);
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => SendAsync(HttpMethod.Post,
            $"/api/staff/bookings/{booking.Id}/check-ins", _factory.StaffToken, new { booking.QrToken })));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<CheckInResponse>()));
        Assert.Equal(results[0], results[1]);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var checkIn = await db.CheckIns.SingleAsync();
        Assert.Equal(CheckInMethod.QR, checkIn.Method);
        Assert.False(checkIn.OutstandingPaymentOverride);
        Assert.Null(checkIn.OverrideReason);
        var history = await db.BookingStatusHistories.SingleAsync();
        Assert.Equal(BookingStatus.Confirmed, history.FromStatus);
        Assert.Equal(BookingStatus.CheckedIn, history.ToStatus);
        Assert.Equal(_factory.StaffUser.Id, history.ChangedByUserId);
        Assert.Single(await db.Notifications.ToListAsync());
        Assert.Equal(2, await db.Payments.CountAsync());
        Assert.Equal(booking.QrToken, (await db.Bookings.SingleAsync()).QrToken);
        await AssertErrorAsync(await SendAsync(HttpMethod.Get, $"/api/bookings/{booking.Id}/qr", _factory.ActiveCustomerToken),
            HttpStatusCode.Conflict, ErrorCodes.QrNotAvailable);
        var verify = await SendAsync(HttpMethod.Post, "/api/staff/check-ins/verify", _factory.StaffToken, new { booking.QrToken });
        Assert.True((await verify.Content.ReadFromJsonAsync<StaffQrVerificationResponse>())!.AlreadyCheckedIn);
        // Idempotency never skips authorization or QR checks.
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{booking.Id}/check-ins",
            _factory.StaffToken, new { qrToken = "wrong" }), HttpStatusCode.BadRequest, ErrorCodes.QrInvalid);
    }

    [Fact]
    public async Task Login_Hold_DepositPending_DepositSucceeded_History_Qr_Collection_CheckIn()
    {
        var customerLogin = await _client.PostAsJsonAsync("/api/auth/login",
            new { emailOrPhone = _factory.ActiveCustomer.PhoneNumber, password = "Demo@123456" });
        customerLogin.EnsureSuccessStatusCode();
        var customerToken = (await customerLogin.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        (await _client.GetAsync($"/api/courts/{_factory.ActiveCourtId}/availability?date={date:yyyy-MM-dd}")).EnsureSuccessStatusCode();
        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)), TimeSpan.FromHours(7));
        var holdResponse = await SendAsync(HttpMethod.Post, "/api/bookings/hold", customerToken,
            new BookingHoldRequest(_factory.ActiveCourtId, new List<DateTimeOffset> { start, start.AddHours(1) }));
        Assert.Equal(HttpStatusCode.Created, holdResponse.StatusCode);
        var hold = (await holdResponse.Content.ReadFromJsonAsync<BookingHoldResponse>())!;
        Assert.Equal("PendingPayment", hold.BookingStatus);
        Assert.Equal("Unpaid", hold.PaymentStatus);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
            var held = await db.Bookings.AsNoTracking().Include(b => b.Slots).SingleAsync(b => b.Id == hold.BookingId);
            Assert.Null(held.QrToken);
            Assert.All(held.Slots, s => Assert.Equal(ReservationState.Held, s.ReservationState));
            // Only reference/authorization data is seeded; payment and booking transitions use HTTP.
            db.StaffAssignments.Add(new StaffAssignment { StaffUserId = _factory.StaffUser.Id, SportCenterId = _factory.CenterId });
            await db.SaveChangesAsync();
        }
        await AssertErrorAsync(await SendAsync(HttpMethod.Get, $"/api/bookings/{hold.BookingId}/qr", customerToken),
            HttpStatusCode.Conflict, ErrorCodes.QrNotAvailable);
        var heldAvailability = await _client.GetFromJsonAsync<CourtAvailabilityDto>(
            $"/api/courts/{_factory.ActiveCourtId}/availability?date={date:yyyy-MM-dd}");
        Assert.Equal(AvailabilitySlotStatus.Held, heldAvailability!.Slots.Single(s => s.StartAt == start).Status);

        var depositResponse = await SendAsync(HttpMethod.Post, $"/api/bookings/{hold.BookingId}/payments/deposit",
            customerToken, new { paymentMethod = "MoMo" });
        depositResponse.EnsureSuccessStatusCode();
        var deposit = (await depositResponse.Content.ReadFromJsonAsync<DepositPaymentResponse>())!;
        Assert.Equal("Pending", deposit.TransactionStatus);
        Assert.Equal("Deposit", deposit.PaymentKind);
        Assert.Equal("Development", deposit.Gateway);
        Assert.Equal(60000, deposit.Amount);
        var pendingStatusResponse = await SendAsync(HttpMethod.Get, $"/api/bookings/{hold.BookingId}/payment-status", customerToken);
        var pendingStatus = (await pendingStatusResponse.Content.ReadFromJsonAsync<BookingPaymentStatusResponse>())!;
        Assert.Equal("PendingPayment", pendingStatus.BookingStatus);
        Assert.Equal("Unpaid", pendingStatus.PaymentStatus);
        Assert.Equal(0, pendingStatus.PaidAmount);
        Assert.Equal("Pending", Assert.Single(pendingStatus.Payments).TransactionStatus);

        var successResponse = await SendAsync(HttpMethod.Post, $"/api/dev/payments/{deposit.PaymentId}/simulate",
            customerToken, new { result = "success" });
        successResponse.EnsureSuccessStatusCode();
        var success = (await successResponse.Content.ReadFromJsonAsync<DepositConfirmationResponse>())!;
        Assert.Equal("Succeeded", success.TransactionStatus);
        Assert.Equal("Confirmed", success.BookingStatus);
        Assert.Equal("DepositPaid", success.PaymentStatus);
        Assert.Equal(60000, success.DepositPaid);
        Assert.Equal(140000, success.RemainingAmount);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
            var confirmed = await db.Bookings.AsNoTracking().Include(b => b.Slots).Include(b => b.Payments)
                .SingleAsync(b => b.Id == hold.BookingId);
            Assert.Equal(BookingStatus.Confirmed, confirmed.BookingStatus);
            Assert.Equal(BookingPaymentStatus.DepositPaid, confirmed.PaymentStatus);
            Assert.Null(confirmed.HoldExpiresAt);
            Assert.False(string.IsNullOrWhiteSpace(confirmed.QrToken));
            Assert.All(confirmed.Slots, s =>
            {
                Assert.Equal(ReservationState.Reserved, s.ReservationState);
                Assert.True(s.IsOccupying);
                Assert.Null(s.HoldExpiresAt);
                Assert.Equal(100000, s.UnitPrice);
            });
            Assert.Equal(PaymentTransactionStatus.Succeeded, Assert.Single(confirmed.Payments).TransactionStatus);
            Assert.Single(await db.BookingStatusHistories.Where(h => h.BookingId == hold.BookingId &&
                h.FromStatus == BookingStatus.PendingPayment && h.ToStatus == BookingStatus.Confirmed).ToListAsync());
        }
        var bookedAvailability = await _client.GetFromJsonAsync<CourtAvailabilityDto>(
            $"/api/courts/{_factory.ActiveCourtId}/availability?date={date:yyyy-MM-dd}");
        Assert.Equal(AvailabilitySlotStatus.Booked, bookedAvailability!.Slots.Single(s => s.StartAt == start).Status);
        var listResponse = await SendAsync(HttpMethod.Get, "/api/bookings?statusGroup=upcoming&pageNumber=1&pageSize=20", customerToken);
        var list = (await listResponse.Content.ReadFromJsonAsync<PagedResult<BookingListItemDto>>())!;
        Assert.Equal(hold.BookingId, Assert.Single(list.Items).BookingId);
        var detailResponse = await SendAsync(HttpMethod.Get, $"/api/bookings/{hold.BookingId}", customerToken);
        Assert.Equal(140000, (await detailResponse.Content.ReadFromJsonAsync<BookingDetailDto>())!.Payment.RemainingAmount);
        var qrResponse = await SendAsync(HttpMethod.Get, $"/api/bookings/{hold.BookingId}/qr", customerToken);
        Assert.True(qrResponse.Headers.CacheControl!.NoStore);
        var qr = (await qrResponse.Content.ReadFromJsonAsync<BookingQrResponse>())!;
        var staffLogin = await _client.PostAsJsonAsync("/api/auth/login",
            new { emailOrPhone = _factory.StaffUser.PhoneNumber, password = "Demo@123456" });
        staffLogin.EnsureSuccessStatusCode();
        var staffToken = (await staffLogin.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
        var verified = await SendAsync(HttpMethod.Post, "/api/staff/check-ins/verify", staffToken, new { qrToken = qr.QrValue });
        Assert.True((await verified.Content.ReadFromJsonAsync<StaffQrVerificationResponse>())!.RequiresRemainingPayment);
        var collected = await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{hold.BookingId}/payments/remaining",
            staffToken, new { paymentMethod = "Cash" });
        Assert.Equal("FullyPaid", (await collected.Content.ReadFromJsonAsync<CollectRemainingPaymentResponse>())!.PaymentStatus);
        var checkedIn = await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{hold.BookingId}/check-ins",
            staffToken, new { qrToken = qr.QrValue });
        Assert.Equal("CheckedIn", (await checkedIn.Content.ReadFromJsonAsync<CheckInResponse>())!.BookingStatus);
        using var finalScope = _factory.Services.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        var final = await finalDb.Bookings.AsNoTracking().SingleAsync(b => b.Id == hold.BookingId);
        Assert.Equal(BookingStatus.CheckedIn, final.BookingStatus);
        Assert.Equal(BookingPaymentStatus.FullyPaid, final.PaymentStatus);
        Assert.Single(await finalDb.CheckIns.Where(c => c.BookingId == hold.BookingId).ToListAsync());
        Assert.Equal(2, await finalDb.Payments.CountAsync(p => p.BookingId == hold.BookingId &&
            p.TransactionStatus == PaymentTransactionStatus.Succeeded));
    }

    [Fact]
    public async Task Swagger_ExposesExactRoutesAndCamelCaseQueryParameters()
    {
        var document = await _client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var paths = document.GetProperty("paths");
        foreach (var path in new[] { "/api/bookings", "/api/bookings/{bookingId}", "/api/bookings/{bookingId}/qr" })
            Assert.True(paths.GetProperty(path).TryGetProperty("get", out _));
        foreach (var path in new[] { "/api/staff/check-ins/verify",
            "/api/staff/bookings/{bookingId}/payments/remaining", "/api/staff/bookings/{bookingId}/check-ins" })
            Assert.True(paths.GetProperty(path).TryGetProperty("post", out _));
        var parameters = paths.GetProperty("/api/bookings").GetProperty("get").GetProperty("parameters")
            .EnumerateArray().Select(p => p.GetProperty("name").GetString()).ToArray();
        Assert.Equal(new[] { "statusGroup", "pageNumber", "pageSize" }, parameters);
        var remainingSchema = document.GetProperty("components").GetProperty("schemas")
            .GetProperty("CollectRemainingPaymentRequest").GetProperty("properties");
        Assert.False(remainingSchema.TryGetProperty("amount", out _));
    }

    [Fact]
    public async Task AssignmentRevokedAfterVerify_IsCheckedAgainForCommands()
    {
        var booking = await SeedBookingAsync(paid: true);
        (await SendAsync(HttpMethod.Post, "/api/staff/check-ins/verify", _factory.StaffToken,
            new { booking.QrToken })).EnsureSuccessStatusCode();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        (await db.StaffAssignments.SingleAsync()).IsActive = false;
        await db.SaveChangesAsync();
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{booking.Id}/check-ins",
            _factory.StaffToken, new { booking.QrToken }), HttpStatusCode.Forbidden, ErrorCodes.StaffNotAssigned);
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{booking.Id}/payments/remaining",
            _factory.StaffToken, new { paymentMethod = "Cash" }), HttpStatusCode.Forbidden, ErrorCodes.StaffNotAssigned);
    }

    [Fact]
    public async Task MissingSettings_RejectsOutstanding_AndFullyPaidNeedsNoOverride()
    {
        var booking = await SeedBookingAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourtGoDbContext>();
        db.SystemSettings.Remove(await db.SystemSettings.SingleAsync());
        await db.SaveChangesAsync();
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{booking.Id}/check-ins",
            _factory.StaffToken, new { booking.QrToken }), HttpStatusCode.InternalServerError, ErrorCodes.SystemSettingsNotFound);
        Assert.Empty(await db.CheckIns.ToListAsync());
        (await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{booking.Id}/payments/remaining",
            _factory.StaffToken, new { paymentMethod = "Cash" })).EnsureSuccessStatusCode();
        var response = await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{booking.Id}/check-ins",
            _factory.StaffToken, new { booking.QrToken, allowOutstandingPayment = true, overrideReason = "Unnecessary" });
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<CheckInResponse>())!;
        Assert.False(result.OutstandingPaymentOverride);
        Assert.Null((await db.CheckIns.SingleAsync()).OverrideReason);
    }

    [Fact]
    public async Task UnknownBookingCommands_Return404()
    {
        await SeedBookingAsync();
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{Guid.NewGuid()}/payments/remaining",
            _factory.StaffToken, new { paymentMethod = "Cash" }), HttpStatusCode.NotFound, ErrorCodes.BookingNotFound);
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, $"/api/staff/bookings/{Guid.NewGuid()}/check-ins",
            _factory.StaffToken, new { qrToken = "unknown" }), HttpStatusCode.NotFound, ErrorCodes.BookingNotFound);
    }

    public void Dispose() { _client.Dispose(); _factory.Dispose(); }
}
