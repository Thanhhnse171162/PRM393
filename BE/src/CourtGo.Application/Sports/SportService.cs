using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;

namespace CourtGo.Application.Sports;

public class SportService : ISportService
{
    private readonly ISportRepository _repository;

    public SportService(ISportRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<SportDto>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var sports = await _repository.GetAllAsync(activeOnly, ct);
        return sports.Select(s => new SportDto(
            s.Id,
            s.Code,
            s.Name,
            s.IconUrl,
            s.DisplayOrder,
            s.IsActive
        )).ToList();
    }

    public async Task<SportDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sport = await _repository.GetByIdAsync(id, ct);
        if (sport is null)
        {
            throw new NotFoundException($"Sport with ID '{id}' was not found.");
        }

        return new SportDto(
            sport.Id,
            sport.Code,
            sport.Name,
            sport.IconUrl,
            sport.DisplayOrder,
            sport.IsActive
        );
    }
}
