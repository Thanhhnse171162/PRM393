namespace CourtGo.Domain.Rules;

/// <summary>
/// Pure business rules for fixed 1-hour slots. See docs/business-rules.md.
/// Availability against the database is checked later by the Application layer.
/// </summary>
public static class SlotRules
{
    public static readonly TimeSpan SlotLength = TimeSpan.FromHours(1);

    /// <summary>
    /// True when the start times are non-empty, aligned to the hour, unique and
    /// form one consecutive 1-hour chain (e.g. 17:00, 18:00, 19:00).
    /// </summary>
    public static bool AreConsecutive(IEnumerable<DateTime> slotStarts)
    {
        var ordered = slotStarts.OrderBy(s => s).ToList();
        if (ordered.Count == 0) return false;
        if (ordered.Any(s => s.Minute != 0 || s.Second != 0 || s.Millisecond != 0)) return false;

        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i] - ordered[i - 1] != SlotLength) return false;
        }
        return true;
    }
}
