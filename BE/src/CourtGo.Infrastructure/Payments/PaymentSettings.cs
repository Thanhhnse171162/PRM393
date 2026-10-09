using CourtGo.Application.Common.Exceptions;

namespace CourtGo.Infrastructure.Payments;

public sealed class PaymentSettings
{
    public string ProviderMode { get; set; } = "Development";
    public MoMoSettings MoMo { get; set; } = new();
    public VnPaySettings VnPay { get; set; } = new();
    public static ConfigurationException Missing() => new("Payment merchant configuration is missing or invalid.", ErrorCodes.PaymentGatewayNotConfigured);
    internal static void Required(params string[] values)
    {
        if (values.Any(v => string.IsNullOrWhiteSpace(v) || v.Contains("REPLACE", StringComparison.OrdinalIgnoreCase))) throw Missing();
    }
    internal static void PublicHttps(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.IsLoopback ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment)) throw Missing();
    }
}
public sealed class MoMoSettings
{
    public string PartnerCode { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string ApiBaseUrl { get; set; } = "https://test-payment.momo.vn";
    public string RedirectUrl { get; set; } = "";
    public string IpnUrl { get; set; } = "";
    public void Validate()
    {
        PaymentSettings.Required(PartnerCode, AccessKey, SecretKey, RedirectUrl, IpnUrl);
        PaymentSettings.PublicHttps(RedirectUrl); PaymentSettings.PublicHttps(IpnUrl);
        if (ApiBaseUrl.TrimEnd('/') is not ("https://test-payment.momo.vn" or "https://payment.momo.vn")) throw PaymentSettings.Missing();
    }
}
public sealed class VnPaySettings
{
    public string TmnCode { get; set; } = "";
    public string HashSecret { get; set; } = "";
    public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string ReturnUrl { get; set; } = "";
    public string IpnUrl { get; set; } = "";
    public string Version { get; set; } = "2.1.0";
    public string Locale { get; set; } = "vn";
    public void Validate()
    {
        PaymentSettings.Required(TmnCode, HashSecret, ReturnUrl, IpnUrl);
        PaymentSettings.PublicHttps(ReturnUrl); PaymentSettings.PublicHttps(IpnUrl);
        if (PaymentUrl is not ("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html" or "https://pay.vnpay.vn/vpcpay.html") ||
            Version != "2.1.0" || Locale is not ("vn" or "en")) throw PaymentSettings.Missing();
    }
}
