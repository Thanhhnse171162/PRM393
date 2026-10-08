namespace CourtGo.Application.Common;

public static class TimeZoneHelper
{
    public const string DefaultTimeZoneId = "Asia/Ho_Chi_Minh";

    public static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            timeZoneId = DefaultTimeZoneId;

        if (TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var tz))
            return tz;

        if (string.Equals(timeZoneId, "Asia/Ho_Chi_Minh", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(timeZoneId, "Asia/Saigon", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(timeZoneId, "Asia/Bangkok", StringComparison.OrdinalIgnoreCase))
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById("SE Asia Standard Time", out var seAsia))
                return seAsia;
        }

        return TimeZoneInfo.CreateCustomTimeZone(timeZoneId, TimeSpan.FromHours(7), timeZoneId, timeZoneId);
    }

    public static bool IsValidTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return false;

        var trimmed = timeZoneId.Trim();

        if (TimeZoneInfo.TryFindSystemTimeZoneById(trimmed, out _))
            return true;

        if (string.Equals(trimmed, "Asia/Ho_Chi_Minh", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Asia/Saigon", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Asia/Bangkok", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "SE Asia Standard Time", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(trimmed, out _))
                return true;
        }
        catch { }

        try
        {
            if (TimeZoneInfo.TryConvertWindowsIdToIanaId(trimmed, out _))
                return true;
        }
        catch { }

        return false;
    }
}
