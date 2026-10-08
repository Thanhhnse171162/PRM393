namespace CourtGo.Application.AdminReports;

public record AdminReportSummaryQuery(
    DateOnly DateFrom,
    DateOnly DateTo,
    Guid? CenterId = null
);

public record AdminReportSummaryResponse(
    DateOnly DateFrom,
    DateOnly DateTo,
    Guid? CenterId,
    int TotalBookings,
    int CompletedBookings,
    int CancelledBookings,
    int NoShowBookings,
    decimal GrossCollected,
    decimal RefundAmount,
    decimal NetRevenue,
    decimal AverageBookingValue,
    decimal OccupancyRate
);

public record AdminRevenueReportQuery(
    DateOnly DateFrom,
    DateOnly DateTo,
    Guid? CenterId = null,
    string? GroupBy = "day"
);

public record AdminRevenueSeriesItemDto(
    string Key,
    string Label,
    decimal GrossCollected,
    decimal RefundAmount,
    decimal NetRevenue,
    int TransactionCount
);

public record AdminRevenueReportResponse(
    DateOnly DateFrom,
    DateOnly DateTo,
    Guid? CenterId,
    string GroupBy,
    decimal GrossCollected,
    decimal RefundAmount,
    decimal NetRevenue,
    List<AdminRevenueSeriesItemDto> Items
);

public record AdminBookingReportQuery(
    DateOnly DateFrom,
    DateOnly DateTo,
    Guid? CenterId = null
);

public record AdminCategoryCountDto(
    string Key,
    string Label,
    int Count
);

public record AdminBookingTrendDto(
    DateOnly Date,
    int TotalBookings,
    int CancelledBookings,
    int NoShowBookings
);

public record AdminBookingReportResponse(
    DateOnly DateFrom,
    DateOnly DateTo,
    Guid? CenterId,
    int TotalBookings,
    int CancelledCount,
    int NoShowCount,
    List<AdminCategoryCountDto> ByStatus,
    List<AdminCategoryCountDto> BySport,
    List<AdminCategoryCountDto> ByCenter,
    List<AdminBookingTrendDto> Trends
);

public record AdminOccupancyReportQuery(
    DateOnly DateFrom,
    DateOnly DateTo,
    Guid? CenterId = null
);

public record AdminCourtOccupancyDto(
    Guid CourtId,
    string CourtName,
    string SportName,
    int BookableMinutes,
    int OccupiedMinutes,
    decimal OccupancyRate
);

public record AdminDayOccupancyDto(
    DateOnly Date,
    int BookableMinutes,
    int OccupiedMinutes,
    decimal OccupancyRate
);

public record AdminOccupancyReportResponse(
    DateOnly DateFrom,
    DateOnly DateTo,
    Guid? CenterId,
    decimal OverallOccupancyRate,
    int TotalBookableMinutes,
    int OccupiedMinutes,
    List<AdminCourtOccupancyDto> ByCourt,
    List<AdminDayOccupancyDto> ByDay
);
