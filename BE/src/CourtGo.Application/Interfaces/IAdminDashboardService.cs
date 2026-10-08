using CourtGo.Application.AdminDashboard;

namespace CourtGo.Application.Interfaces;

public interface IAdminDashboardService
{
    Task<AdminDashboardResponse> GetDashboardAsync(AdminDashboardQuery query, CancellationToken cancellationToken = default);
}
