namespace CourtGo.Api.Payments;

public sealed class VnPayOptions
{
    public const string SectionName = "VnPay";
    public string TmnCode { get; set; } = "";
    public string HashSecret { get; set; } = "";
    public string PaymentUrl { get; set; } = "";
    // This must point to the API return endpoint, which verifies the signature.
    public string ReturnUrl { get; set; } = "";
    // Optional browser destination after the API has verified the result.
    public string FrontendReturnUrl { get; set; } = "";
    public string Version { get; set; } = "2.1.0";
    public string Command { get; set; } = "pay";
    public string CurrCode { get; set; } = "VND";
    public string Locale { get; set; } = "vn";
}
