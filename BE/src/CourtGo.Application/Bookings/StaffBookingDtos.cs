using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CourtGo.Application.Bookings;

public record StaffQrVerificationRequest([Required, MaxLength(150)] string QrToken);

// Reject client-supplied amount/user/center fields instead of silently accepting them.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CollectRemainingPaymentRequest(string? PaymentMethod);

public record CreateCheckInRequest(
    [Required, MaxLength(150)] string QrToken,
    bool AllowOutstandingPayment = false,
    [MaxLength(500)] string? OverrideReason = null);

public record StaffQrVerificationResponse(
    Guid BookingId, string BookingCode, BookingCustomerDto Customer,
    BookingCenterDto Center, BookingCourtDto Court, BookingSportDto Sport,
    DateTimeOffset StartAt, DateTimeOffset EndAt, int DurationMinutes,
    string BookingStatus, string PaymentStatus, BookingPaymentDto Payment,
    bool RequiresRemainingPayment, bool AlreadyCheckedIn);
public record CollectRemainingPaymentResponse(
    Guid BookingId, Guid PaymentId, string PaymentMethod, decimal AmountCollected,
    string PaymentStatus, decimal PaidAmount, decimal RemainingAmount);
public record CheckInResponse(
    Guid BookingId, string BookingCode, string BookingStatus, string PaymentStatus,
    DateTimeOffset CheckedInAt, Guid StaffUserId, bool OutstandingPaymentOverride,
    string CourtName, DateTimeOffset StartAt, DateTimeOffset EndAt);
