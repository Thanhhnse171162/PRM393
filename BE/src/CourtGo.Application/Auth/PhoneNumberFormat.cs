using System.Text.RegularExpressions;

namespace CourtGo.Application.Auth;

public static partial class PhoneNumberFormat
{
    /// <summary>Removes spaces/punctuation; keeps digits and an optional leading '+'.</summary>
    public static string Normalize(string phone)
    {
        var trimmed = phone.Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return trimmed.StartsWith('+') ? "+" + digits : digits;
    }

    public static bool IsValid(string normalized) => PhoneRegex().IsMatch(normalized);

    [GeneratedRegex(@"^\+?\d{9,15}$")]
    private static partial Regex PhoneRegex();
}
