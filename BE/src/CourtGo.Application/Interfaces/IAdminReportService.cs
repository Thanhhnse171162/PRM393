using CourtGo.Application.AdminReports;

namespace CourtGo.Application.Interfaces;

public interface IAdminReportService
{
    Task<AdminReportSummaryResponse> GetSummaryReportAsync(AdminReportSummaryQuery query, CancellationToken cancellationToken = default);
    Task<AdminRevenueReportResponse> GetRevenueReportAsync(AdminRevenueReportQuery query, CancellationToken cancellationToken = default);
    Task<AdminBookingReportResponse> GetBookingReportAsync(AdminBookingReportQuery query, CancellationToken cancellationToken = default);
    Task<AdminOccupancyReportResponse> GetOccupancyReportAsync(AdminOccupancyReportQuery query, CancellationToken cancellationToken = default);
}
