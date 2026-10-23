using CourtGo.Api.Payments;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CourtGo.IntegrationTests;

public class VnPayGatewayTests
{
    private static readonly VnPayGateway Gateway = new(Options.Create(new VnPayOptions
    {
        TmnCode = "TESTCODE",
        HashSecret = "test-secret-only",
        PaymentUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
        ReturnUrl = "http://localhost:5080/api/payments/vnpay/return"
    }));

    [Fact]
    public void CreatesSignedUrlWithAmountInSmallestVnPayUnit()
    {
        var id = Guid.NewGuid();
        var url = Gateway.CreatePaymentUrl(id, 72_000m, "127.0.0.1", new DateTime(2026, 10, 23, 10, 0, 0, DateTimeKind.Utc));
        var query = new QueryCollection(Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(url).Query));

        Assert.Equal("7200000", query["vnp_Amount"]);
        Assert.Equal(id.ToString("N"), query["vnp_TxnRef"]);
        Assert.Equal("20261023170000", query["vnp_CreateDate"]);
    }

    [Fact]
    public void RejectsTamperedAndUnsignedCallbacks()
    {
        var url = Gateway.CreatePaymentUrl(Guid.NewGuid(), 72_000m, "127.0.0.1", DateTime.UtcNow);
        var fields = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(url).Query)
            .ToDictionary(p => p.Key, p => p.Value);
        fields["vnp_ResponseCode"] = "00";
        fields["vnp_TransactionStatus"] = "00";
        Assert.False(Gateway.TryValidate(new QueryCollection(fields), out _));
        fields.Remove("vnp_SecureHash");
        Assert.False(Gateway.TryValidate(new QueryCollection(fields), out _));
    }

    [Fact]
    public void AcceptsSignedSuccessfulCallback()
    {
        var id = Guid.NewGuid();
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Amount"] = "7200000",
            ["vnp_ResponseCode"] = "00",
            ["vnp_TmnCode"] = "TESTCODE",
            ["vnp_TransactionNo"] = "12345",
            ["vnp_TransactionStatus"] = "00",
            ["vnp_TxnRef"] = id.ToString("N")
        };
        var signedText = string.Join("&", fields.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        fields["vnp_SecureHash"] = Convert.ToHexString(HMACSHA512.HashData(
            Encoding.UTF8.GetBytes("test-secret-only"), Encoding.UTF8.GetBytes(signedText)));
        var query = new QueryCollection(fields.ToDictionary(p => p.Key,
            p => new Microsoft.Extensions.Primitives.StringValues(p.Value)));

        Assert.True(Gateway.TryValidate(query, out var result));
        Assert.Equal(id, result.PaymentId);
        Assert.Equal(7_200_000, result.Amount);
        Assert.True(result.IsSuccessful);
    }
}
