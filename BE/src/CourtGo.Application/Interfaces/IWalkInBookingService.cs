using CourtGo.Application.Operations;
namespace CourtGo.Application.Interfaces;
public interface IWalkInBookingService
{
    Task<StaffBookingDetail> CreateWalkInAsync(Guid staffId, WalkInBookingRequest request, CancellationToken cancellationToken = default);
}
