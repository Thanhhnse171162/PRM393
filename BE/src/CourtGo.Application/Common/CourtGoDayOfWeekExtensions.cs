using CourtGo.Domain.Enums;

namespace CourtGo.Application.Common;

public static class CourtGoDayOfWeekExtensions
{
    public static CourtGoDayOfWeek ToCourtGoDayOfWeek(this DayOfWeek dow) => dow switch
    {
        DayOfWeek.Monday => CourtGoDayOfWeek.Monday,
        DayOfWeek.Tuesday => CourtGoDayOfWeek.Tuesday,
        DayOfWeek.Wednesday => CourtGoDayOfWeek.Wednesday,
        DayOfWeek.Thursday => CourtGoDayOfWeek.Thursday,
        DayOfWeek.Friday => CourtGoDayOfWeek.Friday,
        DayOfWeek.Saturday => CourtGoDayOfWeek.Saturday,
        DayOfWeek.Sunday => CourtGoDayOfWeek.Sunday,
        _ => throw new ArgumentOutOfRangeException(nameof(dow), dow, null)
    };
}
