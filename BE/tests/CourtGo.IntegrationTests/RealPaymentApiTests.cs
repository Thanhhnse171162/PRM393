using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CourtGo.Application.Bookings;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Payments;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CourtGo.IntegrationTests;

public sealed class RealPaymentApiTests
{
    private sealed class Factory : BookingHoldApiTests.Factory
    {
        public QueryHandler Query { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment("Development");
            builder.UseSetting("Payments:ProviderMode", "Production");
            builder.UseSetting("Payments:VnPay:TmnCode", "TESTCODE");
            builder.UseSetting("Payments:VnPay:HashSecret", "test-only-secret-not-a-merchant");
            builder.UseSetting("Payments:VnPay:ReturnUrl", "https://example.test/api/payments/vnpay/return");
            builder.UseSetting("Payments:VnPay:IpnUrl", "https://example.test/api/payments/vnpay/ipn");
            builder.ConfigureServices(s => s.AddHttpClient("VnPayQuery").ConfigurePrimaryHttpMessageHandler(() => Query));
        }
    }
    private sealed class QueryHandler : HttpMessageHandler
    {
        public int Calls;
        public decimal Amount;
        public bool InvalidHash;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            var q = (await request.Content!.ReadFromJsonAsync<Dictionary<string, string>>(ct))!;
            var r = new Dictionary<string, string> {
                ["vnp_ResponseId"]="response", ["vnp_Command"]="querydr", ["vnp_ResponseCode"]="00", ["vnp_Message"]="OK",
                ["vnp_TmnCode"]="TESTCODE", ["vnp_TxnRef"]=q["vnp_TxnRef"], ["vnp_Amount"]=PaymentSignatures.Number(Amount*100),
                ["vnp_BankCode"]="NCB", ["vnp_PayDate"]="20261024124120", ["vnp_TransactionNo"]="123456",
                ["vnp_TransactionType"]="01", ["vnp_TransactionStatus"]="00", ["vnp_OrderInfo"]="CourtGo deposit",
                ["vnp_PromotionCode"]="", ["vnp_PromotionAmount"]=""
            };
            r["vnp_SecureHash"] = PaymentSignatures.Sha512("test-only-secret-not-a-merchant", string.Join("|", r.Values));
            if (InvalidHash) r["vnp_SecureHash"] = "invalid";
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(r) };
        }
    }
    private static async Task<DepositPaymentResponse> Start(Factory f, HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", f.ActiveCustomerToken);
        var start = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(2).AddHours(10), TimeSpan.Zero);
        var hold = await client.PostAsJsonAsync("/api/bookings/hold", new { courtId = f.ActiveCourtId, slotStartAts = new[] { start } });
        hold.EnsureSuccessStatusCode();
        var booking = (await hold.Content.ReadFromJsonAsync<BookingHoldResponse>())!;
        var response = await client.PostAsJsonAsync($"/api/bookings/{booking.BookingId}/payments/deposit", new { paymentMethod = "VNPay" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DepositPaymentResponse>())!;
    }
    private static string Callback(DepositPaymentResponse p, long receipt = 123456, decimal? amount = null, string code = "00")
    {
        var fields = new Dictionary<string, string> {
            ["vnp_TmnCode"] = "TESTCODE", ["vnp_TxnRef"] = p.PaymentId.ToString("N"),
            ["vnp_Amount"] = PaymentSignatures.Number((amount ?? p.Amount) * 100),
            ["vnp_ResponseCode"] = code, ["vnp_TransactionStatus"] = code == "00" ? "00" : "02",
            ["vnp_TransactionNo"] = receipt.ToString()
        };
        var raw = PaymentSignatures.VnPayCanonical(fields);
        return raw + "&vnp_SecureHash=" + PaymentSignatures.Sha512("test-only-secret-not-a-merchant", raw);
    }
    private static async Task<string?> Ack(HttpClient client, string query)
        => (await (await client.GetAsync("/api/payments/vnpay/ipn?" + query)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("RspCode").GetString();

    [Fact]
    public async Task ReturnNeverFinalizes_IpnConfirmsExactlyOnce_AndDevelopmentRoutesAreRemoved()
    {
        using var f = new Factory(); using var client = f.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var p = await Start(f, client);
        Assert.StartsWith("https://sandbox.vnpayment.vn/", p.PaymentUrl);
        var again = await client.PostAsJsonAsync($"/api/bookings/{p.BookingId}/payments/deposit", new { paymentMethod = "VNPay" });
        Assert.Equal(p.PaymentUrl, (await again.Content.ReadFromJsonAsync<DepositPaymentResponse>())!.PaymentUrl);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/dev/payments/{p.PaymentId}/simulate", new { result = "success" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/dev/refunds/{p.PaymentId}/simulate", new { result = "success" })).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        var returned = await client.GetAsync("/api/payments/vnpay/return?" + Callback(p));
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
        Assert.Contains($"courtgo://payment-result?bookingId={p.BookingId:D}", await returned.Content.ReadAsStringAsync());
        Assert.Equal("no-referrer", returned.Headers.GetValues("Referrer-Policy").Single());
        using (var db = f.CreateDbContext()) Assert.Equal(BookingStatus.PendingPayment, (await db.Bookings.SingleAsync(b => b.Id == p.BookingId)).BookingStatus);
        Assert.Equal("97", await Ack(client, Callback(p) + "f"));
        Assert.Equal("04", await Ack(client, Callback(p, amount: p.Amount + 1)));
        var results = await Task.WhenAll(Ack(client, Callback(p)), Ack(client, Callback(p)));
        Assert.All(results, r => Assert.Contains(r, new[] { "00", "02" }));
        Assert.Equal("02", await Ack(client, Callback(p)));
        using var final = f.CreateDbContext();
        var booking = await final.Bookings.SingleAsync(b => b.Id == p.BookingId);
        Assert.Equal(BookingStatus.Confirmed, booking.BookingStatus); Assert.NotNull(booking.QrToken);
        Assert.Single(await final.BookingStatusHistories.Where(h => h.BookingId == booking.Id && h.ToStatus == BookingStatus.Confirmed).ToListAsync());
        Assert.All(await final.BookingSlots.Where(s => s.BookingId == booking.Id).ToListAsync(), s => Assert.Equal(ReservationState.Reserved, s.ReservationState));
        Assert.Equal("VNPay:123456", (await final.Payments.SingleAsync(x => x.Id == p.PaymentId)).ProviderReceiptId);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/payments/vnpay/return?" + Callback(p))).StatusCode);
    }

    [Fact]
    public async Task MissingIpnAndReturn_QueryConfirmsOnce_ConcurrentIpnDoesNotDoubleCharge()
    {
        using var f = new Factory(); using var client = f.CreateClient(); var p = await Start(f, client); f.Query.Amount = p.Amount;
        var calls = await Task.WhenAll(client.PostAsJsonAsync($"/api/bookings/{p.BookingId}/payments/reconcile", new { }),
            client.PostAsJsonAsync($"/api/bookings/{p.BookingId}/payments/reconcile", new { }),
            client.GetAsync("/api/payments/vnpay/ipn?" + Callback(p)));
        Assert.All(calls, r => r.EnsureSuccessStatusCode());
        using var db = f.CreateDbContext();
        Assert.Equal(BookingStatus.Confirmed, (await db.Bookings.SingleAsync(b => b.Id == p.BookingId)).BookingStatus);
        Assert.Single(await db.Payments.Where(x => x.BookingId == p.BookingId).ToListAsync());
        Assert.Single(await db.BookingStatusHistories.Where(h => h.BookingId == p.BookingId && h.ToStatus == BookingStatus.Confirmed).ToListAsync());
    }

    [Fact]
    public async Task QueryWithoutAnyCallbackRecoversDeposit_AndChecksOwnershipAndSignature()
    {
        using var f = new Factory(); using var client = f.CreateClient(); var p = await Start(f, client); f.Query.Amount = p.Amount;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", f.SecondCustomerToken);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/bookings/{p.BookingId}/payments/reconcile", new { })).StatusCode);
        Assert.Equal(0, f.Query.Calls);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", f.ActiveCustomerToken);
        var response = await client.PostAsJsonAsync($"/api/bookings/{p.BookingId}/payments/reconcile", new { });
        response.EnsureSuccessStatusCode();
        var state = (await response.Content.ReadFromJsonAsync<BookingPaymentStatusResponse>())!;
        Assert.Equal("Confirmed", state.BookingStatus); Assert.Equal("DepositPaid", state.PaymentStatus);
        Assert.Equal(state.TotalAmount - state.DepositAmount, state.RemainingAmount);
        await client.PostAsJsonAsync($"/api/bookings/{p.BookingId}/payments/reconcile", new { });
        Assert.Equal(1, f.Query.Calls);
    }

    [Fact]
    public async Task InvalidQuerySignatureCannotConfirmOrChangePendingPayment()
    {
        using var f = new Factory(); using var client = f.CreateClient(); var p = await Start(f, client);
        f.Query.Amount = p.Amount; f.Query.InvalidHash = true;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/bookings/{p.BookingId}/payments/reconcile", new { })).StatusCode);
        using var db = f.CreateDbContext();
        Assert.Equal(PaymentTransactionStatus.Pending, (await db.Payments.SingleAsync(x => x.Id == p.PaymentId)).TransactionStatus);
        Assert.Null((await db.Bookings.SingleAsync(b => b.Id == p.BookingId)).QrToken);
    }

    [Fact]
    public async Task QueryAfterExpiryAndRebookingRecordsMoneyWithoutTakingNewCustomersSlot()
    {
        using var f = new Factory(); using var client = f.CreateClient(); var p = await Start(f, client); f.Query.Amount = p.Amount;
        using (var db = f.CreateDbContext()) {
            var b = await db.Bookings.SingleAsync(b => b.Id == p.BookingId);
            b.HoldExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            foreach (var s in await db.BookingSlots.Where(s => s.BookingId == b.Id).ToListAsync()) s.HoldExpiresAt = b.HoldExpiresAt;
            await db.SaveChangesAsync();
        }
        // Availability/hold uses the existing expiration service and reserves the released slot for another customer.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", f.SecondCustomerToken);
        var start = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(2).AddHours(10), TimeSpan.Zero);
        var hold = await client.PostAsJsonAsync("/api/bookings/hold", new { courtId = f.ActiveCourtId, slotStartAts = new[] { start } });
        hold.EnsureSuccessStatusCode(); var other = (await hold.Content.ReadFromJsonAsync<BookingHoldResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", f.ActiveCustomerToken);
        (await client.PostAsJsonAsync($"/api/bookings/{p.BookingId}/payments/reconcile", new { })).EnsureSuccessStatusCode();
        Assert.Equal("02", await Ack(client, Callback(p)));
        using var final = f.CreateDbContext(); var original = await final.Bookings.SingleAsync(b => b.Id == p.BookingId);
        Assert.Equal(BookingStatus.Expired, original.BookingStatus); Assert.Equal(BookingPaymentStatus.RefundPending, original.PaymentStatus);
        Assert.Null(original.QrToken);
        Assert.All(await final.BookingSlots.Where(s => s.BookingId == p.BookingId).ToListAsync(), s => Assert.False(s.IsOccupying));
        Assert.All(await final.BookingSlots.Where(s => s.BookingId == other.BookingId).ToListAsync(), s => Assert.True(s.IsOccupying));
        Assert.Single(await final.Payments.Where(x => x.RelatedPaymentId == p.PaymentId && x.TransactionStatus == PaymentTransactionStatus.Pending).ToListAsync());
    }

    [Fact]
    public async Task UnknownReferenceAndOutOfOrderFailureCannotOverwriteSuccess()
    {
        using var f = new Factory(); using var client = f.CreateClient(); var p = await Start(f, client);
        Assert.Equal("01", await Ack(client, Callback(p with { PaymentId = Guid.NewGuid() })));
        Assert.Equal("00", await Ack(client, Callback(p)));
        Assert.Equal("99", await Ack(client, Callback(p, code: "24")));
        using var db = f.CreateDbContext();
        Assert.Equal(PaymentTransactionStatus.Succeeded, (await db.Payments.SingleAsync(x => x.Id == p.PaymentId)).TransactionStatus);
    }

    [Fact]
    public async Task LateSuccessCreatesOnePendingRefund_WithoutQrOrSlotResurrection()
    {
        using var f = new Factory(); using var client = f.CreateClient(); var p = await Start(f, client);
        using (var db = f.CreateDbContext())
        {
            var booking = await db.Bookings.SingleAsync(b => b.Id == p.BookingId);
            booking.HoldExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }
        Assert.Equal("00", await Ack(client, Callback(p)));
        Assert.Equal("02", await Ack(client, Callback(p)));
        using var final = f.CreateDbContext();
        var expired = await final.Bookings.SingleAsync(b => b.Id == p.BookingId);
        Assert.Equal(BookingStatus.Expired, expired.BookingStatus); Assert.Null(expired.QrToken);
        Assert.Equal(BookingPaymentStatus.RefundPending, expired.PaymentStatus);
        var refund = Assert.Single(await final.Payments.Where(x => x.RelatedPaymentId == p.PaymentId).ToListAsync());
        Assert.Equal(PaymentTransactionStatus.Pending, refund.TransactionStatus); Assert.Null(refund.PaidAt);
        Assert.All(await final.BookingSlots.Where(s => s.BookingId == expired.Id).ToListAsync(), s => Assert.False(s.IsOccupying));
    }
    [Fact]
    public async Task FailedProviderAttemptKeepsValidHoldForRetry()
    {
        using var f = new Factory(); using var client = f.CreateClient(); var p = await Start(f, client);
        Assert.Equal("00", await Ack(client, Callback(p, code: "24")));
        var retry = await client.PostAsJsonAsync($"/api/bookings/{p.BookingId}/payments/deposit", new { paymentMethod = "VNPay" });
        retry.EnsureSuccessStatusCode();
        Assert.NotEqual(p.PaymentId, (await retry.Content.ReadFromJsonAsync<DepositPaymentResponse>())!.PaymentId);
        using var db = f.CreateDbContext();
        Assert.Equal(BookingStatus.PendingPayment, (await db.Bookings.SingleAsync(b => b.Id == p.BookingId)).BookingStatus);
    }
}
