using CourtGo.Application.Bookings;

namespace CourtGo.Application.Interfaces;

public interface IDepositPaymentService
{
    Task<DepositPaymentResponse> StartDepositAsync(Guid customerUserId, Guid bookingId,
        StartDepositPaymentRequest request, CancellationToken cancellationToken = default);
    Task<BookingPaymentStatusResponse> GetPaymentStatusAsync(Guid customerUserId, Guid bookingId,
        CancellationToken cancellationToken = default);
    /// <summary>Only trusted provider adapters may call this after signature/result verification.</summary>
    Task<DepositConfirmationResponse> ApplyVerifiedResultAsync(VerifiedPaymentResult result, Guid? customerUserId = null,
        CancellationToken cancellationToken = default);
}
public interface IDevelopmentPaymentSimulationService
{
    Task<DepositConfirmationResponse> SimulateAsync(Guid customerUserId, Guid paymentId,
        SimulatePaymentRequest request, CancellationToken cancellationToken = default);
}
public interface IQrTokenGenerator
{
    string GenerateToken();
}
