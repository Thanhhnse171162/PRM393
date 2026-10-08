using System.Text.Json.Serialization;
using CourtGo.Application.Common;

namespace CourtGo.Application.Operations;

public record AdminCancellationRequestQuery(
    string? Status = null,
    Guid? CenterId = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20);

public record AdminCancellationRequestSummaryDto(
    Guid RequestId,
    Guid BookingId,
    string BookingCode,
    string Status,
    string Reason,
    decimal CalculatedRefundAmount,
    decimal? ApprovedRefundAmount,
    string CustomerNameSnapshot,
    string CustomerPhoneSnapshot,
    string CenterNameSnapshot,
    string CourtNameSnapshot,
    string SportNameSnapshot,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt);

public record CustomerSnapshotDto(
    Guid? CustomerUserId,
    string Name,
    string Phone,
    string? Email);

public record FacilitySnapshotDto(
    string CenterName,
    string CourtName,
    string SportName);

public record CancellationPolicySnapshotDto(
    Guid? PolicyId,
    string? PolicyName,
    decimal? MatchedRefundPercent);

public record PaymentSummaryDto(
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RefundedAmount,
    string PaymentStatus,
    IReadOnlyList<PaymentItemDto> Payments);

public record PaymentItemDto(
    Guid PaymentId,
    string PaymentKind,
    string PaymentMethod,
    decimal Amount,
    string TransactionStatus,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt);

public record AdminCancellationRequestDetailDto(
    Guid RequestId,
    string RequestStatus,
    string Reason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    Guid? ProcessedByUserId,
    Guid BookingId,
    string BookingCode,
    string BookingStatus,
    CustomerSnapshotDto Customer,
    FacilitySnapshotDto Facility,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal CalculatedRefundAmount,
    decimal? ApprovedRefundAmount,
    CancellationPolicySnapshotDto? Policy,
    PaymentSummaryDto PaymentSummary);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CancellationDecisionRequest(
    string Decision,
    decimal? ApprovedRefundAmount = null,
    string? Reason = null);

public record CancellationDecisionResponse(
    Guid RequestId,
    Guid BookingId,
    string Decision,
    string RequestStatus,
    string BookingStatus,
    decimal? ApprovedRefundAmount,
    Guid? RefundPaymentId,
    DateTimeOffset ProcessedAt);
