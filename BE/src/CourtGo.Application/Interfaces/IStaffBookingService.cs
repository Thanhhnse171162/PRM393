using CourtGo.Application.Bookings;

namespace CourtGo.Application.Interfaces;

public interface IStaffBookingService
{
    Task<StaffQrVerificationResponse> VerifyQrAsync(Guid staffUserId, StaffQrVerificationRequest request, CancellationToken cancellationToken = default);
    Task<CollectRemainingPaymentResponse> CollectRemainingPaymentAsync(Guid staffUserId, Guid bookingId, CollectRemainingPaymentRequest request, CancellationToken cancellationToken = default);
    Task<CheckInResponse> CheckInAsync(Guid staffUserId, Guid bookingId, CreateCheckInRequest request, CancellationToken cancellationToken = default);
}
