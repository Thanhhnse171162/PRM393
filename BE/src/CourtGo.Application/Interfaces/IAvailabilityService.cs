using CourtGo.Application.Availability;

namespace CourtGo.Application.Interfaces;

public interface IAvailabilityService
{
    Task<CourtAvailabilityDto> GetCourtAvailabilityAsync(
        Guid courtId,
        DateOnly date,
        CancellationToken ct = default);
}
