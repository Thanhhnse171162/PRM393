using System.Net;
using System.Net.Http.Json;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Payments;

namespace CourtGo.UnitTests;

public sealed class VnPayQueryTests
{
    private const string Secret = "query-test-secret";
    private static PaymentSettings Settings() => new() { ProviderMode = "Production", VnPay = new() {
        TmnCode = "TESTCODE", HashSecret = Secret, ReturnUrl = "https://example.test/return", IpnUrl = "https://example.test/ipn" } };
    private static Payment Payment() { var p = new Payment { PaymentKind = PaymentKind.Deposit, PaymentMethod = PaymentMethod.VNPay, Amount = 90000 };
        p.ProviderTransactionId = p.Id.ToString("N");
        p.CheckoutUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_TmnCode=TESTCODE&vnp_TxnRef={p.ProviderTransactionId}&vnp_Amount=9000000&vnp_CurrCode=VND&vnp_CreateDate=20261024124050";
        return p; }
    internal static Dictionary<string, string> Response(Payment p, string state = "00") => new() {
        ["vnp_ResponseId"] = "response1", ["vnp_Command"] = "querydr", ["vnp_ResponseCode"] = "00", ["vnp_Message"] = "Success",
        ["vnp_TmnCode"] = "TESTCODE", ["vnp_TxnRef"] = p.ProviderTransactionId!, ["vnp_Amount"] = "9000000", ["vnp_BankCode"] = "NCB",
        ["vnp_PayDate"] = "20261024124120", ["vnp_TransactionNo"] = "15713716", ["vnp_TransactionType"] = "01",
        ["vnp_TransactionStatus"] = state, ["vnp_OrderInfo"] = "CourtGo deposit"
    };
    private static void Sign(Dictionary<string, string> r) {
        // Independent explicit ordering from VNPAY's QueryDR response specification, including empty promotion fields.
        r["vnp_SecureHash"] = PaymentSignatures.Sha512(Secret,
            $"{r["vnp_ResponseId"]}|{r["vnp_Command"]}|{r["vnp_ResponseCode"]}|{r["vnp_Message"]}|{r["vnp_TmnCode"]}|{r["vnp_TxnRef"]}|{r["vnp_Amount"]}|{r["vnp_BankCode"]}|{r["vnp_PayDate"]}|{r["vnp_TransactionNo"]}|{r["vnp_TransactionType"]}|{r["vnp_TransactionStatus"]}|{r["vnp_OrderInfo"]}||"); }
    [Theory]
    [InlineData("00", PaymentTransactionStatus.Succeeded)]
    [InlineData("01", PaymentTransactionStatus.Pending)]
    [InlineData("06", PaymentTransactionStatus.Pending)]
    [InlineData("99", PaymentTransactionStatus.Pending)]
    [InlineData("02", PaymentTransactionStatus.Failed)]
    public void QuerySuccessDoesNotMeanPaymentSuccess(string status, PaymentTransactionStatus expected) {
        var p = Payment(); var r = Response(p, status); Sign(r);
        var verified = new VnPayTransactionQuery(new HttpClient(), Settings(), TimeProvider.System).VerifyResponse(p, r);
        Assert.Equal(expected, verified.TransactionStatus);
        Assert.Equal(expected == PaymentTransactionStatus.Succeeded ? "VNPay:15713716" : null, verified.ProviderReceiptId);
    }
    [Theory]
    [InlineData("vnp_Amount", "9000100")]
    [InlineData("vnp_Amount", "9000001")]
    [InlineData("vnp_TxnRef", "other-order")]
    [InlineData("vnp_TmnCode", "OTHER")]
    [InlineData("vnp_TransactionType", "02")]
    [InlineData("vnp_TransactionNo", "0")]
    [InlineData("vnp_Command", "refund")]
    public void ValidSignatureCannotOverrideRecordOrTransactionType(string key, string value) {
        var p = Payment(); var r = Response(p); r[key] = value; Sign(r);
        Assert.Throws<ValidationException>(() => new VnPayTransactionQuery(new HttpClient(), Settings(), TimeProvider.System).VerifyResponse(p, r));
    }
    [Fact]
    public void TamperedResponseIsRejected() {
        var p = Payment(); var r = Response(p); Sign(r); r["vnp_TransactionStatus"] = "02";
        Assert.Throws<ValidationException>(() => new VnPayTransactionQuery(new HttpClient(), Settings(), TimeProvider.System).VerifyResponse(p, r));
    }
    private sealed class Handler(Payment p) : HttpMessageHandler {
        public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
            Calls++; Assert.Equal("https://sandbox.vnpayment.vn/merchant_webapi/api/transaction", request.RequestUri!.AbsoluteUri);
            Assert.True(request.Content!.Headers.ContentLength > 0);
            Assert.Contains("CourtGo/1.0", request.Headers.UserAgent.ToString());
            var f = (await request.Content!.ReadFromJsonAsync<Dictionary<string, string>>(ct))!;
            Assert.Equal("20261024124050", f["vnp_TransactionDate"]);
            Assert.Equal(p.ProviderTransactionId, f["vnp_TxnRef"]);
            Assert.Equal(PaymentSignatures.Sha512(Secret, $"{f["vnp_RequestId"]}|2.1.0|querydr|TESTCODE|{p.ProviderTransactionId}|20261024124050|{f["vnp_CreateDate"]}|127.0.0.1|CourtGo payment reconciliation"), f["vnp_SecureHash"]);
            var r = Response(p); Sign(r); return new(HttpStatusCode.OK) { Content = JsonContent.Create(r) };
        }
    }
    [Fact]
    public async Task QueryUsesOriginalDateAndVerifiedVndAmount() {
        var p = Payment(); var h = new Handler(p); var q = new VnPayTransactionQuery(new HttpClient(h), Settings(), TimeProvider.System);
        Assert.Equal(PaymentTransactionStatus.Succeeded, (await q.QueryAsync(p)).TransactionStatus);
        p.CheckoutUrl = p.CheckoutUrl!.Replace("VND", "USD");
        await Assert.ThrowsAsync<ValidationException>(() => q.QueryAsync(p)); Assert.Equal(1, h.Calls);
    }
}
