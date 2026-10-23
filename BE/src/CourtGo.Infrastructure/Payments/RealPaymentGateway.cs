using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Enums;

namespace CourtGo.Infrastructure.Payments;

/// <summary>Central method resolver; no provider selection is spread through booking services.</summary>
public sealed class RealPaymentGateway(HttpClient http, PaymentSettings settings, string clientIp = "127.0.0.1") : IPaymentGateway
{
    public string Name => "Production";
    public bool Supports(PaymentMethod method) => method is PaymentMethod.MoMo or PaymentMethod.VNPay;
    public void ValidateConfiguration(PaymentMethod method)
    {
        if (settings.ProviderMode != "Production") throw PaymentSettings.Missing();
        if (method == PaymentMethod.MoMo) settings.MoMo.Validate();
        else if (method == PaymentMethod.VNPay) settings.VnPay.Validate();
        else throw PaymentSettings.Missing();
    }
    public string GetReference(GatewayPaymentRequest request) => request.PaymentId.ToString("N");
    public async Task<GatewayPaymentAttempt> CreateDepositAsync(GatewayPaymentRequest request, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration(request.PaymentMethod);
        if (request.Amount <= 0 || decimal.Truncate(request.Amount) != request.Amount)
            throw new ValidationException("Gateway requires a positive whole VND amount.", ErrorCodes.PaymentAmountMismatch);
        return request.PaymentMethod switch
        {
            PaymentMethod.MoMo => await CreateMoMoAsync(request, cancellationToken),
            PaymentMethod.VNPay => CreateVnPay(request),
            _ => throw PaymentSettings.Missing()
        };
    }
    // No merchant refund permission exists yet. Queue a durable manual refund, never report it as paid.
    public Task<GatewayPaymentAttempt> CreateRefundAsync(GatewayPaymentRequest request, Guid originalPaymentId, CancellationToken cancellationToken = default)
        => Task.FromResult(new GatewayPaymentAttempt($"refund-{request.PaymentId:N}", null, "ManualReconciliation"));

    private async Task<GatewayPaymentAttempt> CreateMoMoAsync(GatewayPaymentRequest r, CancellationToken ct)
    {
        if (r.Amount < 1000 || r.Amount > 50000000) throw new ValidationException("MoMo amount is outside the supported range.", ErrorCodes.PaymentAmountMismatch);
        var m = settings.MoMo;
        var id = GetReference(r);
        var amount = PaymentSignatures.Number(r.Amount);
        var info = $"CourtGo deposit {r.BookingId:N}";
        var raw = $"accessKey={m.AccessKey}&amount={amount}&extraData=&ipnUrl={m.IpnUrl}&orderId={id}&orderInfo={info}&partnerCode={m.PartnerCode}&redirectUrl={m.RedirectUrl}&requestId={id}&requestType=captureWallet";
        using var response = await http.PostAsJsonAsync(m.ApiBaseUrl.TrimEnd('/') + "/v2/gateway/api/create", new
        {
            partnerCode = m.PartnerCode, requestId = id, orderId = id, amount = (long)r.Amount, orderInfo = info,
            redirectUrl = m.RedirectUrl, ipnUrl = m.IpnUrl, requestType = "captureWallet", extraData = "", lang = "vi",
            autoCapture = true, signature = PaymentSignatures.Sha256(m.SecretKey, raw)
        }, ct);
        if (!response.IsSuccessStatusCode) throw new ConflictException("Gateway is unavailable; retry this same payment attempt.", ErrorCodes.PaymentStateConflict);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = json.RootElement;
        if (!root.TryGetProperty("resultCode", out var code) || code.GetInt32() != 0 ||
            !root.TryGetProperty("orderId", out var order) || order.GetString() != id ||
            !root.TryGetProperty("requestId", out var requestId) || requestId.GetString() != id ||
            !root.TryGetProperty("amount", out var receivedAmount) || receivedAmount.GetDecimal() != r.Amount ||
            !root.TryGetProperty("payUrl", out var payUrl))
            throw new ConflictException("Gateway has not returned a verified checkout session; keep the same attempt for reconciliation.", ErrorCodes.PaymentStateConflict);
        var url = payUrl.GetString();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !(uri.Host == "momo.vn" || uri.Host.EndsWith(".momo.vn", StringComparison.OrdinalIgnoreCase)) || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ValidationException("Gateway checkout URL is invalid.", ErrorCodes.PaymentVerificationFailed);
        return new(id, url, "MoMo");
    }
    private GatewayPaymentAttempt CreateVnPay(GatewayPaymentRequest r)
    {
        var v = settings.VnPay;
        if (r.CreatedAt is null || r.ExpiresAt is null || r.ExpiresAt <= r.CreatedAt) throw new ValidationException("Payment expiry is required.");
        string Date(DateTimeOffset time) => time.ToOffset(TimeSpan.FromHours(7)).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var fields = new Dictionary<string, string>
        {
            ["vnp_Version"] = v.Version, ["vnp_Command"] = "pay", ["vnp_TmnCode"] = v.TmnCode,
            ["vnp_Amount"] = PaymentSignatures.Number(checked(r.Amount * 100)), ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = GetReference(r), ["vnp_OrderInfo"] = $"CourtGo deposit {r.BookingId:N}",
            ["vnp_OrderType"] = "other", ["vnp_Locale"] = v.Locale, ["vnp_ReturnUrl"] = v.ReturnUrl,
            ["vnp_IpAddr"] = SanitizeIp(clientIp), ["vnp_CreateDate"] = Date(r.CreatedAt.Value), ["vnp_ExpireDate"] = Date(r.ExpiresAt.Value)
        };
        var raw = PaymentSignatures.VnPayCanonical(fields);
        return new(GetReference(r), v.PaymentUrl + "?" + raw + "&vnp_SecureHash=" + PaymentSignatures.Sha512(v.HashSecret, raw), "VNPay");
    }

    private static string SanitizeIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return "127.0.0.1";
        var candidate = ip.Trim();
        if (candidate.Contains(',')) candidate = candidate.Split(',')[0].Trim();
        if (candidate.Contains(':') && System.Net.IPAddress.TryParse(candidate, out var parsedV6) && parsedV6.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            if (parsedV6.IsIPv4MappedToIPv6)
            {
                var v4 = parsedV6.MapToIPv4().ToString();
                return v4 == "0.0.0.1" ? "127.0.0.1" : v4;
            }
            if (candidate == "::1") return "127.0.0.1";
            return candidate.Length <= 45 ? candidate : "127.0.0.1";
        }
        if (candidate.Contains(':'))
        {
            var host = candidate.Split(':')[0];
            if (System.Net.IPAddress.TryParse(host, out _)) return host;
        }
        if (System.Net.IPAddress.TryParse(candidate, out var addr))
        {
            var str = addr.ToString();
            return str == "0.0.0.1" ? "127.0.0.1" : str;
        }
        return "127.0.0.1";
    }
}

