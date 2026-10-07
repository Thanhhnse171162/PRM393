namespace CourtGo.Application.SportCenters;

public record OperatingHourDto(
    string DayOfWeek,
    TimeOnly? OpenTime,
    TimeOnly? CloseTime,
    bool IsClosed
);
