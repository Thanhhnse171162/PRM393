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
    private static PaymentSettings Settings() => new() { ProviderMode = "Production", MoMo = new() {
        PartnerCode = "TEST", AccessKey = "ACCESS", SecretKey = "test-only-secret", RedirectUrl = "https://example.test/return", IpnUrl = "https://example.test/ipn"
    } };
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
}
