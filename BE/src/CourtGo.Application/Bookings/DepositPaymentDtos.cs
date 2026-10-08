using System.Text.Json.Serialization;

namespace CourtGo.Application.Bookings;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record StartDepositPaymentRequest(string? PaymentMethod);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record SimulatePaymentRequest(string? Result);

public record DepositPaymentResponse(Guid PaymentId, Guid BookingId, string BookingCode,
    string PaymentKind, string PaymentMethod, decimal Amount, string TransactionStatus,
    string? ProviderTransactionId, string? PaymentUrl, DateTimeOffset? HoldExpiresAt, string Gateway);
public record DepositConfirmationResponse(Guid PaymentId, string TransactionStatus,
    Guid BookingId, string BookingCode, string BookingStatus, string PaymentStatus,
    string CourtName, string CenterName, string SportName, DateTimeOffset StartAt,
    DateTimeOffset EndAt, int DurationMinutes, decimal TotalAmount, decimal DepositPaid, decimal RemainingAmount);
public record PaymentTransactionDto(Guid PaymentId, string PaymentKind, string PaymentMethod,
    decimal Amount, string TransactionStatus, DateTimeOffset? PaidAt);
public record BookingPaymentStatusResponse(Guid BookingId, string BookingCode, string BookingStatus,
    string PaymentStatus, decimal TotalAmount, decimal DepositAmount, decimal PaidAmount,
    decimal RemainingAmount, IReadOnlyList<PaymentTransactionDto> Payments);
