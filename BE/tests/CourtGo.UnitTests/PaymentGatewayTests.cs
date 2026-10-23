using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Payments;

namespace CourtGo.UnitTests;

public sealed class PaymentGatewayTests
{
    private static PaymentSettings Settings() => new() {
        ProviderMode = "Production",
        MoMo = new() {
            PartnerCode = "TEST", AccessKey = "ACCESS", SecretKey = "test-only-secret", RedirectUrl = "https://example.test/return", IpnUrl = "https://example.test/ipn"
        },
        VnPay = new() {
            TmnCode = "TESTTMN1", HashSecret = "test-only-vnpay-hash-secret-value", PaymentUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
            ReturnUrl = "https://example.test/vnpay/return", IpnUrl = "https://example.test/vnpay/ipn", Version = "2.1.0", Locale = "vn"
        }
    };
    private sealed class Handler : HttpMessageHandler
    {
        public List<JsonElement> Requests { get; } = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = await request.Content!.ReadFromJsonAsync<JsonElement>(ct); Requests.Add(body);
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(new {
                resultCode = 0, orderId = body.GetProperty("orderId").GetString(), requestId = body.GetProperty("requestId").GetString(),
                amount = body.GetProperty("amount").GetInt64(), payUrl = "https://test-payment.momo.vn/checkout/test"
            }) };
        }
    }
    [Fact]
    public async Task MoMoUsesStableIdServerAmountAndOfficialSignatureFields()
    {
        var handler = new Handler(); var settings = Settings();
        var gateway = new RealPaymentGateway(new HttpClient(handler), settings);
        var request = new GatewayPaymentRequest(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.MoMo, 60000);
        var first = await gateway.CreateDepositAsync(request); var second = await gateway.CreateDepositAsync(request);
        Assert.Equal(first, second); Assert.Equal(2, handler.Requests.Count);
        var body = handler.Requests[0]; var id = request.PaymentId.ToString("N");
        var raw = $"accessKey=ACCESS&amount=60000&extraData=&ipnUrl=https://example.test/ipn&orderId={id}&orderInfo=CourtGo deposit {request.BookingId:N}&partnerCode=TEST&redirectUrl=https://example.test/return&requestId={id}&requestType=captureWallet";
        Assert.Equal(PaymentSignatures.Sha256("test-only-secret", raw), body.GetProperty("signature").GetString());
        Assert.True(body.GetProperty("autoCapture").GetBoolean());
        Assert.Equal(body.GetProperty("signature").GetString(), handler.Requests[1].GetProperty("signature").GetString());
    }
    [Fact]
    public async Task MissingCredentialsFailClosedWithoutHttp()
    {
        var handler = new Handler(); var gateway = new RealPaymentGateway(new HttpClient(handler), new() { ProviderMode = "Production" });
        var error = await Assert.ThrowsAsync<ConfigurationException>(() => gateway.CreateDepositAsync(new(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.MoMo, 60000)));
        Assert.Equal(ErrorCodes.PaymentGatewayNotConfigured, error.Code); Assert.Empty(handler.Requests);
    }
    [Theory]
    [InlineData("0", PaymentTransactionStatus.Succeeded)]
    [InlineData("1006", PaymentTransactionStatus.Cancelled)]
    [InlineData("1001", PaymentTransactionStatus.Failed)]
    [InlineData("7000", PaymentTransactionStatus.Pending)]
    [InlineData("9000", PaymentTransactionStatus.Pending)]
    public void MoMoVerifiesMerchantSignatureAmountAndFinalState(string code, PaymentTransactionStatus expected)
    {
        var id = Guid.NewGuid().ToString("N");
        var f = new Dictionary<string, string> { ["amount"]="60000", ["extraData"]="", ["message"]="OK", ["orderId"]=id,
            ["orderInfo"]="CourtGo", ["orderType"]="momo_wallet", ["partnerCode"]="TEST", ["payType"]="qr",
            ["requestId"]=id, ["responseTime"]="1791500000000", ["resultCode"]=code, ["transId"]="123456" };
        var raw = "accessKey=ACCESS&" + string.Join("&", f.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{x.Key}={x.Value}"));
        f["signature"] = PaymentSignatures.Sha256("test-only-secret", raw);
        var verifier = new PaymentNotificationVerifier(Settings());
        Assert.Equal(expected, verifier.VerifyMoMo(f).TransactionStatus);
        f["amount"] = "60001";
        Assert.Throws<ValidationException>(() => verifier.VerifyMoMo(f));
    }
    [Fact]
    public void HmacMatchesIndependentStandardVectorAndRejectsMalformedHex()
    {
        Assert.Equal("f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8", PaymentSignatures.Sha256("key", "The quick brown fox jumps over the lazy dog"));
        Assert.False(PaymentSignatures.Matches(new string('a', 64), new string('z', 64)));
        Assert.Equal("vnp_A=a+b&vnp_Z=%26", PaymentSignatures.VnPayCanonical(new Dictionary<string,string> { ["vnp_Z"]="&", ["vnp_A"]="a b", ["vnp_SecureHash"]="ignored" }));
    }
    [Fact]
    public async Task VnPayBuildsCompliantUrlAndHmacSha512Signature()
    {
        var settings = Settings();
        var gateway = new RealPaymentGateway(new HttpClient(), settings, "118.69.182.40");
        var paymentId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var expiresAt = createdAt.AddMinutes(15);
        var request = new GatewayPaymentRequest(paymentId, bookingId, PaymentMethod.VNPay, 60000, createdAt, expiresAt);

        var attempt = await gateway.CreateDepositAsync(request);

        Assert.Equal(paymentId.ToString("N"), attempt.ProviderTransactionId);
        Assert.Equal("VNPay", attempt.Gateway);
        Assert.NotNull(attempt.PaymentUrl);

        var uri = new Uri(attempt.PaymentUrl);
        Assert.Equal("sandbox.vnpayment.vn", uri.Host);
        Assert.Equal("/paymentv2/vpcpay.html", uri.AbsolutePath);

        // Parse query string parameters
        var query = uri.Query.TrimStart('?').Split('&')
            .Select(part => part.Split('=', 2))
            .ToDictionary(parts => WebUtility.UrlDecode(parts[0]), parts => parts.Length > 1 ? WebUtility.UrlDecode(parts[1]) : "");

        Assert.Equal("2.1.0", query["vnp_Version"]);
        Assert.Equal("pay", query["vnp_Command"]);
        Assert.Equal("TESTTMN1", query["vnp_TmnCode"]);
        Assert.Equal("6000000", query["vnp_Amount"]);
        Assert.Equal("VND", query["vnp_CurrCode"]);
        Assert.Equal(paymentId.ToString("N"), query["vnp_TxnRef"]);
        Assert.Equal($"CourtGo deposit {bookingId:N}", query["vnp_OrderInfo"]);
        Assert.Equal("other", query["vnp_OrderType"]);
        Assert.Equal("vn", query["vnp_Locale"]);
        Assert.Equal("https://example.test/vnpay/return", query["vnp_ReturnUrl"]);
        Assert.Equal("118.69.182.40", query["vnp_IpAddr"]);

        // Validate GMT+7 timestamps format: yyyyMMddHHmmss
        Assert.Matches(@"^\d{14}$", query["vnp_CreateDate"]);
        Assert.Matches(@"^\d{14}$", query["vnp_ExpireDate"]);
        Assert.True(string.CompareOrdinal(query["vnp_ExpireDate"], query["vnp_CreateDate"]) > 0);

        // Recompute hash over canonical parameters and verify
        var canonical = PaymentSignatures.VnPayCanonical(query);
        var expectedHash = PaymentSignatures.Sha512("test-only-vnpay-hash-secret-value", canonical);
        Assert.Equal(expectedHash, query["vnp_SecureHash"]);
    }
    [Theory]
    [InlineData("::1", "127.0.0.1")]
    [InlineData("0.0.0.1", "127.0.0.1")]
    [InlineData("", "127.0.0.1")]
    [InlineData(null, "127.0.0.1")]
    [InlineData("113.161.45.67:8080", "113.161.45.67")]
    [InlineData("113.161.45.67, 10.0.0.1", "113.161.45.67")]
    [InlineData("203.0.113.195", "203.0.113.195")]
    public async Task VnPaySanitizesClientIpAddress(string? inputIp, string expectedIp)
    {
        var settings = Settings();
        var gateway = new RealPaymentGateway(new HttpClient(), settings, inputIp!);
        var request = new GatewayPaymentRequest(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.VNPay, 50000, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(15));
        var attempt = await gateway.CreateDepositAsync(request);
        var uri = new Uri(attempt.PaymentUrl!);
        var query = uri.Query.TrimStart('?').Split('&')
            .Select(part => part.Split('=', 2))
            .ToDictionary(parts => Uri.UnescapeDataString(parts[0]), parts => parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "");
        Assert.Equal(expectedIp, query["vnp_IpAddr"]);
    }
    [Fact]
    public async Task VnPayRequiresValidExpiry()
    {
        var gateway = new RealPaymentGateway(new HttpClient(), Settings());
        var now = DateTimeOffset.UtcNow;
        await Assert.ThrowsAsync<ValidationException>(() => gateway.CreateDepositAsync(new(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.VNPay, 50000, null, now.AddMinutes(15))));
        await Assert.ThrowsAsync<ValidationException>(() => gateway.CreateDepositAsync(new(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.VNPay, 50000, now, null)));
        await Assert.ThrowsAsync<ValidationException>(() => gateway.CreateDepositAsync(new(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.VNPay, 50000, now, now.AddMinutes(-5))));
    }
    [Theory]
    [InlineData("00", "00", PaymentTransactionStatus.Succeeded)]
    [InlineData("24", "02", PaymentTransactionStatus.Cancelled)]
    [InlineData("01", "01", PaymentTransactionStatus.Pending)]
    [InlineData("05", "05", PaymentTransactionStatus.Pending)]
    [InlineData("06", "06", PaymentTransactionStatus.Pending)]
    [InlineData("07", "02", PaymentTransactionStatus.Failed)]
    [InlineData("09", "02", PaymentTransactionStatus.Failed)]
    public void VnPayVerifiesIpnSignatureAndStatus(string responseCode, string transactionStatus, PaymentTransactionStatus expected)
    {
        var settings = Settings();
        var paymentId = Guid.NewGuid().ToString("N");
        var fields = new Dictionary<string, string>
        {
            ["vnp_TmnCode"] = "TESTTMN1",
            ["vnp_Amount"] = "6000000",
            ["vnp_BankCode"] = "NCB",
            ["vnp_BankTranNo"] = "VNP14321234",
            ["vnp_CardType"] = "ATM",
            ["vnp_PayDate"] = "20261023160000",
            ["vnp_OrderInfo"] = "CourtGo deposit",
            ["vnp_TransactionNo"] = "14321234",
            ["vnp_ResponseCode"] = responseCode,
            ["vnp_TransactionStatus"] = transactionStatus,
            ["vnp_TxnRef"] = paymentId
        };
        var canonical = PaymentSignatures.VnPayCanonical(fields);
        fields["vnp_SecureHash"] = PaymentSignatures.Sha512("test-only-vnpay-hash-secret-value", canonical);

        var verifier = new PaymentNotificationVerifier(settings);
        var result = verifier.VerifyVnPay(fields);

        Assert.Equal(expected, result.TransactionStatus);
        Assert.Equal(60000m, result.Amount);
        Assert.Equal(Guid.ParseExact(paymentId, "N"), result.PaymentId);

        // Tampering with amount must fail validation
        fields["vnp_Amount"] = "6000100";
        Assert.Throws<ValidationException>(() => verifier.VerifyVnPay(fields));
    }
}
