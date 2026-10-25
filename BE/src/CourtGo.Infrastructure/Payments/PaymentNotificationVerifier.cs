using System.Globalization;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Enums;

namespace CourtGo.Infrastructure.Payments;

public sealed class PaymentNotificationVerifier(PaymentSettings settings)
{
    public VerifiedPaymentResult VerifyMoMo(IReadOnlyDictionary<string, string> fields)
    {
        if (settings.ProviderMode != "Production") throw PaymentSettings.Missing();
        var m = settings.MoMo; m.Validate();
        string Get(string key) => fields.TryGetValue(key, out var value) ? value : throw Invalid();
        var raw = $"accessKey={m.AccessKey}&amount={Get("amount")}&extraData={Get("extraData")}&message={Get("message")}&orderId={Get("orderId")}&orderInfo={Get("orderInfo")}&orderType={Get("orderType")}&partnerCode={Get("partnerCode")}&payType={Get("payType")}&requestId={Get("requestId")}&responseTime={Get("responseTime")}&resultCode={Get("resultCode")}&transId={Get("transId")}";
        if (!PaymentSignatures.Matches(PaymentSignatures.Sha256(m.SecretKey, raw), Get("signature")) ||
            Get("partnerCode") != m.PartnerCode || Get("requestId") != Get("orderId") ||
            !Guid.TryParseExact(Get("orderId"), "N", out var paymentId) ||
            !long.TryParse(Get("amount"), NumberStyles.None, CultureInfo.InvariantCulture, out var amount) || amount <= 0 ||
            !int.TryParse(Get("resultCode"), out var code)) throw Invalid();
        var status = code switch
        {
            0 => PaymentTransactionStatus.Succeeded,
            1003 or 1006 or 1017 => PaymentTransactionStatus.Cancelled,
            98 or 99 or 1001 or 1002 or 1004 or 1005 or 1007 or 1026 or 2019 or 4001 or 4002 or 4100 => PaymentTransactionStatus.Failed,
            _ => PaymentTransactionStatus.Pending // Unknown/nonfinal results never finalize or release a hold.
        };
        var receipt = Receipt("MoMo", Get("transId"), status);
        return new(paymentId, Get("orderId"), amount, status, receipt, "MoMo");
    }
    public VerifiedPaymentResult VerifyVnPay(IReadOnlyDictionary<string, string> fields)
    {
        if (settings.ProviderMode != "Production") throw PaymentSettings.Missing();
        var v = settings.VnPay; v.Validate();
        string Get(string key) => fields.TryGetValue(key, out var value) ? value : throw Invalid();
        if (!PaymentSignatures.Matches(PaymentSignatures.Sha512(v.HashSecret, PaymentSignatures.VnPayCanonical(fields)), Get("vnp_SecureHash")) ||
            Get("vnp_TmnCode") != v.TmnCode || (fields.TryGetValue("vnp_CurrCode", out var currency) && currency != "VND") || !Guid.TryParseExact(Get("vnp_TxnRef"), "N", out var id) ||
            !long.TryParse(Get("vnp_Amount"), NumberStyles.None, CultureInfo.InvariantCulture, out var scaled) || scaled <= 0 || scaled % 100 != 0)
            throw Invalid();
        var response = Get("vnp_ResponseCode");
        var transaction = Get("vnp_TransactionStatus");
        var status = response == "00" && transaction == "00" ? PaymentTransactionStatus.Succeeded
            : response == "24" ? PaymentTransactionStatus.Cancelled
            : transaction is "02" or "04" or "07" ? PaymentTransactionStatus.Failed : PaymentTransactionStatus.Pending;
        return new(id, Get("vnp_TxnRef"), scaled / 100m, status,
            Receipt("VNPay", Get("vnp_TransactionNo"), status), "VNPay");
    }
    private static string? Receipt(string gateway, string value, PaymentTransactionStatus status)
    {
        if (status != PaymentTransactionStatus.Succeeded) return null;
        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number <= 0) throw Invalid();
        return $"{gateway}:{number}";
    }
    private static ValidationException Invalid() => new("Payment notification verification failed.", ErrorCodes.PaymentVerificationFailed);
}
