namespace CourtGo.Application.AdminDashboard;

public record AdminDashboardQuery(
    DateOnly? Date = null,
    Guid? CenterId = null
);

public record AdminDashboardResponse(
    DateOnly Date,
    string TimeZoneId,
    Guid? CenterId,
    int TotalBookings,
    int PendingBookings,
    int ConfirmedBookings,
    int CheckedInBookings,
    int InProgressBookings,
    int CompletedBookings,
    int CancelledBookings,
    int NoShowBookings,
    decimal TodayRevenue,
    decimal RefundAmount,
    int ActiveCenters,
    int ActiveCourts,
    decimal OccupancyRate,
    int UpcomingBookings,
    int PendingCancellationRequests
);
