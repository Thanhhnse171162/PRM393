using CourtGo.Application.Bookings;
using CourtGo.Application.Operations;
namespace CourtGo.Application.Interfaces;
public interface IStaffCourtService
{
    Task<PagedResult<CourtStateDto>> GetCourtsAsync(Guid staffId, int pageNumber, int pageSize, CancellationToken ct);
    Task<CourtStateDto> GetCourtAsync(Guid staffId, Guid courtId, CancellationToken ct);
    Task<PagedResult<CourtBlockDto>> GetBlocksAsync(Guid staffId, Guid courtId, int pageNumber, int pageSize, CancellationToken ct);
    Task<CourtBlockDto> CreateBlockAsync(Guid staffId, Guid courtId, CreateCourtBlockRequest request, CancellationToken ct);
    Task RemoveBlockAsync(Guid staffId, Guid courtId, Guid blockId, CancellationToken ct);
}
