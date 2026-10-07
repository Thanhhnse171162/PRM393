using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;

namespace CourtGo.Application.Courts;

public class CourtService : ICourtService
{
    private readonly ICourtRepository _repository;

    public CourtService(ICourtRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CourtDto>> GetAllAsync(
        Guid? sportCenterId = null,
        Guid? sportId = null,
        CancellationToken ct = default)
    {
        var courts = await _repository.GetAllAsync(sportCenterId, sportId, activeOnly: true, ct);

        return courts.Select(c => new CourtDto(
            c.Id,
            c.SportCenterId,
            c.SportCenter?.Name ?? string.Empty,
            c.SportId,
            c.Sport?.Name ?? string.Empty,
            c.Code,
            c.Name,
            c.SurfaceType,
            c.Description,
            c.CoverImageUrl,
            c.BasePricePerHour,
            c.Status.ToString()
        )).ToList();
    }

    public async Task<CourtDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var court = await _repository.GetByIdAsync(id, ct);
        if (court is null || court.Status != CourtStatus.Active)
        {
            throw new NotFoundException($"Court with ID '{id}' was not found.");
        }

        return new CourtDto(
            court.Id,
            court.SportCenterId,
            court.SportCenter?.Name ?? string.Empty,
            court.SportId,
            court.Sport?.Name ?? string.Empty,
            court.Code,
            court.Name,
            court.SurfaceType,
            court.Description,
            court.CoverImageUrl,
            court.BasePricePerHour,
            court.Status.ToString()
        );
    }
}
