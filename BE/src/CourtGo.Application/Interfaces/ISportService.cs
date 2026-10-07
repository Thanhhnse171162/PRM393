using CourtGo.Application.Sports;

namespace CourtGo.Application.Interfaces;

public interface ISportService
{
    Task<IReadOnlyList<SportDto>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<SportDto> GetByIdAsync(Guid id, CancellationToken ct = default);
}
