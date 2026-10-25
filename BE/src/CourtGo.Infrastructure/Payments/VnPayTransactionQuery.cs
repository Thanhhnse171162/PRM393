using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;

namespace CourtGo.Infrastructure.Payments;

/// <summary>Official QueryDR protocol. Query success is distinct from payment success.</summary>
public sealed class VnPayTransactionQuery(HttpClient http, PaymentSettings settings, TimeProvider time)
{
    public async Task<VerifiedPaymentResult> QueryAsync(Payment payment, CancellationToken ct = default)
    {
        var v = settings.VnPay;
        if (settings.ProviderMode != "Production") throw PaymentSettings.Missing();
        v.Validate();
        if (payment.PaymentMethod != PaymentMethod.VNPay || payment.PaymentKind != PaymentKind.Deposit ||
            payment.ProviderTransactionId != payment.Id.ToString("N") || payment.Amount <= 0 ||
            !Uri.TryCreate(payment.CheckoutUrl, UriKind.Absolute, out var checkout) ||
            checkout.GetLeftPart(UriPartial.Path) != v.PaymentUrl) throw Invalid();
        // Use the ORIGINAL creation date, never the time of the query or the provider pay date.
        var original = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in checkout.Query.TrimStart('?').Split('&'))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length != 2 || !original.TryAdd(WebUtility.UrlDecode(parts[0]), WebUtility.UrlDecode(parts[1]))) throw Invalid();
        }
        string Stored(string key) => original.GetValueOrDefault(key) ?? throw Invalid();
        if (Stored("vnp_CurrCode") != "VND" || Stored("vnp_TmnCode") != v.TmnCode ||
            Stored("vnp_TxnRef") != payment.ProviderTransactionId ||
            !decimal.TryParse(Stored("vnp_Amount"), NumberStyles.None, CultureInfo.InvariantCulture, out var originalAmount) ||
            originalAmount != payment.Amount * 100 ||
            !DateTime.TryParseExact(Stored("vnp_CreateDate"), "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) throw Invalid();
        var fields = new Dictionary<string, string> {
            ["vnp_RequestId"] = Guid.NewGuid().ToString("N"), ["vnp_Version"] = v.Version,
            ["vnp_Command"] = "querydr", ["vnp_TmnCode"] = v.TmnCode,
            ["vnp_TxnRef"] = payment.ProviderTransactionId, ["vnp_TransactionDate"] = Stored("vnp_CreateDate"),
            ["vnp_CreateDate"] = time.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_IpAddr"] = "127.0.0.1", ["vnp_OrderInfo"] = "CourtGo payment reconciliation"
        };
        fields["vnp_SecureHash"] = PaymentSignatures.Sha512(v.HashSecret, string.Join("|", fields.Values));
        // Fixed endpoint matched to the checkout environment; credentials cannot be sent to an arbitrary URL.
        var endpoint = checkout.Host == "sandbox.vnpayment.vn"
            ? "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction"
            : "https://pay.vnpay.vn/merchant_webapi/api/transaction";
        try
        {
            // Send a bounded request with an explicit application identity and Content-Length.
            using var content = JsonContent.Create(fields);
            await content.LoadIntoBufferAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
            request.Headers.UserAgent.ParseAdd("CourtGo/1.0");
            request.Headers.Accept.ParseAdd("application/json");
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) throw new ConflictException($"VNPAY query HTTP {(int)response.StatusCode}.", "PAYMENT_QUERY_UNAVAILABLE");
            // Bounded response, duplicate JSON fields rejected before signature verification.
            await response.Content.LoadIntoBufferAsync(32768);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (json.RootElement.ValueKind != JsonValueKind.Object) throw Invalid();
            foreach (var property in json.RootElement.EnumerateObject())
                if (property.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number) ||
                    !result.TryAdd(property.Name, property.Value.ToString())) throw new ValidationException("VNPAY query response contains invalid or duplicate fields.", ErrorCodes.PaymentVerificationFailed);
            return VerifyResponse(payment, result);
        }
        catch (HttpRequestException) { throw Unavailable(); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw Unavailable(); }
        catch (JsonException) { throw Invalid(); }
    }

    public VerifiedPaymentResult VerifyResponse(Payment payment, IReadOnlyDictionary<string, string> fields)
    {
        string Get(string key) => fields.TryGetValue(key, out var value) ? value : "";
        string[] signed = ["vnp_ResponseId", "vnp_Command", "vnp_ResponseCode", "vnp_Message", "vnp_TmnCode",
            "vnp_TxnRef", "vnp_Amount", "vnp_BankCode", "vnp_PayDate", "vnp_TransactionNo", "vnp_TransactionType",
            "vnp_TransactionStatus", "vnp_OrderInfo", "vnp_PromotionCode", "vnp_PromotionAmount"];
        if (Get("vnp_SecureHash").Length == 0)
        {
            var code = Get("vnp_ResponseCode");
            if (code.Length != 2 || !code.All(char.IsAsciiDigit)) code = "unknown";
            throw new ValidationException($"VNPAY query response is unsigned (code {code}); no state change allowed.", ErrorCodes.PaymentVerificationFailed);
        }
        if (!PaymentSignatures.Matches(PaymentSignatures.Sha512(settings.VnPay.HashSecret, string.Join("|", signed.Select(Get))), Get("vnp_SecureHash")))
            throw new ValidationException(Get("vnp_SecureHash").Length == 0 ? "VNPAY query response has no signature." : "VNPAY query response signature is invalid.", ErrorCodes.PaymentVerificationFailed);
        if (Get("vnp_ResponseCode") != "00") throw Unavailable();
        if (string.IsNullOrWhiteSpace(Get("vnp_ResponseId")) || Get("vnp_Command") is not ("" or "querydr") ||
            Get("vnp_TmnCode") != settings.VnPay.TmnCode || Get("vnp_TxnRef") != payment.ProviderTransactionId ||
            Get("vnp_TxnRef") != payment.Id.ToString("N") || Get("vnp_TransactionType") != "01" ||
            !long.TryParse(Get("vnp_Amount"), NumberStyles.None, CultureInfo.InvariantCulture, out var scaled) ||
            scaled <= 0 || scaled % 100 != 0 || scaled / 100m != payment.Amount) throw Invalid();
        // QueryDR has no signed currency field; QueryAsync verifies VND on the stored original checkout.
        var status = Get("vnp_TransactionStatus") switch {
            "00" => PaymentTransactionStatus.Succeeded,
            "02" or "04" or "07" => PaymentTransactionStatus.Failed,
            _ => PaymentTransactionStatus.Pending // Unknown/refund-in-progress results never confirm a booking.
        };
        string? receipt = null;
        if (status == PaymentTransactionStatus.Succeeded)
        {
            if (!long.TryParse(Get("vnp_TransactionNo"), NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number <= 0) throw Invalid();
            receipt = $"VNPay:{number}";
        }
        return new(payment.Id, payment.ProviderTransactionId!, payment.Amount, status, receipt, "VNPay");
    }
    private static ValidationException Invalid() => new("VNPAY transaction query verification failed.", ErrorCodes.PaymentVerificationFailed);
    private static ConflictException Unavailable() => new("VNPAY has not provided a verifiable transaction result. Please check again later.", "PAYMENT_QUERY_UNAVAILABLE");
}
