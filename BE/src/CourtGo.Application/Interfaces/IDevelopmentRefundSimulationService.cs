using CourtGo.Application.Bookings;
using CourtGo.Application.Operations;
namespace CourtGo.Application.Interfaces;
public interface IDevelopmentRefundSimulationService
{
    Task<CancellationRequestDto> SimulateAsync(Guid paymentId, SimulatePaymentRequest request, CancellationToken ct);
}
