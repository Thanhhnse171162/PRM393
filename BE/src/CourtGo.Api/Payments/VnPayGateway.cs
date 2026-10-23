using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CourtGo.Api.Payments;

public sealed class VnPayGateway(IOptions<VnPayOptions> options)
{
    private readonly VnPayOptions _options = options.Value;

    public string FrontendReturnUrl => _options.FrontendReturnUrl;

    public string CreatePaymentUrl(Guid paymentId, decimal amount, string ipAddress, DateTime utcNow)
    {
        ValidateConfiguration();
        if (amount <= 0 || amount != decimal.Truncate(amount))
            throw new ArgumentOutOfRangeException(nameof(amount), "VNPay requires a positive whole-VND amount.");

        var now = utcNow.AddHours(7);
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _options.Version,
            ["vnp_Command"] = _options.Command,
            ["vnp_TmnCode"] = _options.TmnCode,
            ["vnp_Amount"] = (amount * 100).ToString("0", CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = _options.CurrCode,
            ["vnp_TxnRef"] = paymentId.ToString("N"),
            ["vnp_OrderInfo"] = $"CourtGo deposit {paymentId:N}",
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = _options.Locale,
            ["vnp_ReturnUrl"] = _options.ReturnUrl,
            ["vnp_IpAddr"] = ipAddress,
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_ExpireDate"] = now.AddMinutes(15).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)
        };
        var query = CanonicalQuery(fields);
        var separator = _options.PaymentUrl.Contains('?') ? "&" : "?";
        return $"{_options.PaymentUrl}{separator}{query}&vnp_SecureHash={Sign(query)}";
    }

    public bool TryValidate(IQueryCollection query, out VnPayResult result)
    {
        result = default!;
        ValidateConfiguration();
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in query)
        {
            if (pair.Value.Count != 1 || !pair.Key.StartsWith("vnp_", StringComparison.Ordinal)) return false;
            if (pair.Key is not ("vnp_SecureHash" or "vnp_SecureHashType"))
                fields[pair.Key] = pair.Value.ToString();
        }

        var supplied = query["vnp_SecureHash"].ToString();
        if (supplied.Length != 128) return false;
        byte[] suppliedBytes;
        try { suppliedBytes = Convert.FromHexString(supplied); }
        catch (FormatException) { return false; }
        var expected = Sign(CanonicalQuery(fields));
        if (!CryptographicOperations.FixedTimeEquals(
                suppliedBytes, Convert.FromHexString(expected))) return false;

        if (!fields.TryGetValue("vnp_TmnCode", out var tmnCode) || tmnCode != _options.TmnCode ||
            !fields.TryGetValue("vnp_TxnRef", out var reference) ||
            !Guid.TryParseExact(reference, "N", out var paymentId) ||
            !fields.TryGetValue("vnp_Amount", out var rawAmount) ||
            !long.TryParse(rawAmount, NumberStyles.None, CultureInfo.InvariantCulture, out var amount) ||
            !fields.TryGetValue("vnp_ResponseCode", out var responseCode) ||
            !fields.TryGetValue("vnp_TransactionStatus", out var transactionStatus)) return false;

        result = new VnPayResult(paymentId, amount, responseCode, transactionStatus,
            fields.GetValueOrDefault("vnp_TransactionNo"));
        return true;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.TmnCode) || string.IsNullOrWhiteSpace(_options.HashSecret) ||
            !Uri.TryCreate(_options.PaymentUrl, UriKind.Absolute, out var paymentUrl) || paymentUrl.Scheme != "https" ||
            !Uri.TryCreate(_options.ReturnUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException("VnPay configuration is incomplete. Configure TmnCode, HashSecret, PaymentUrl and the API ReturnUrl.");
    }

    private string Sign(string value)
    {
        var bytes = HMACSHA512.HashData(Encoding.UTF8.GetBytes(_options.HashSecret), Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string CanonicalQuery(IDictionary<string, string> fields) => string.Join("&",
        fields.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"{Encode(p.Key)}={Encode(p.Value)}"));

    private static string Encode(string value) => Uri.EscapeDataString(value).Replace("%20", "+");
}

public sealed record VnPayResult(Guid PaymentId, long Amount, string ResponseCode,
    string TransactionStatus, string? TransactionNumber)
{
    public bool IsSuccessful => ResponseCode == "00" && TransactionStatus == "00";
}
