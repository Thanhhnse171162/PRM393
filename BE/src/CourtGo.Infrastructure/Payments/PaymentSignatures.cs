using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace CourtGo.Infrastructure.Payments;

public static class PaymentSignatures
{
    public static string Sha256(string secret, string raw) => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
    public static string Sha512(string secret, string raw) => Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
    public static bool Matches(string expected, string? supplied)
    {
        if (supplied is null || supplied.Length != expected.Length) return false;
        try { return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(expected), Convert.FromHexString(supplied)); }
        catch (FormatException) { return false; }
    }
    public static string VnPayCanonical(IEnumerable<KeyValuePair<string, string>> fields) => string.Join("&", fields
        .Where(p => p.Key.StartsWith("vnp_", StringComparison.Ordinal) && p.Key is not ("vnp_SecureHash" or "vnp_SecureHashType") && !string.IsNullOrEmpty(p.Value))
        .OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value)}"));
    public static string Number(decimal value) => value.ToString("0", CultureInfo.InvariantCulture);
}
