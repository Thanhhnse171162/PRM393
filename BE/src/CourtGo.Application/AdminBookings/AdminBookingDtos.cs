using System.ComponentModel.DataAnnotations;
using CourtGo.Domain.Enums;

namespace CourtGo.Application.AdminBookings;

public record AdminBookingQuery(
    Guid? CenterId = null,
    Guid? SportId = null,
    Guid? CourtId = null,
    string? Status = null,
    string? PaymentStatus = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20
);

public record AdminBookingListItemResponse(
    Guid BookingId,
    string BookingCode,
    string CustomerName,
    string CustomerPhone,
    Guid SportCenterId,
    string SportCenterName,
    Guid CourtId,
    string CourtName,
    Guid SportId,
    string SportName,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    string BookingStatus,
    string PaymentStatus,
    string Source,
    DateTimeOffset CreatedAt
);

public record AdminCustomerSnapshotDto(
    Guid? CustomerUserId,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail
);

public record AdminFacilitySnapshotDto(
    Guid SportCenterId,
    string SportCenterName,
    Guid CourtId,
    string CourtName,
    Guid SportId,
    string SportName
);

public record AdminBookingSlotDetailDto(
    Guid SlotId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    decimal UnitPrice,
    string ReservationState
);

public record AdminFinancialSummaryDto(
    decimal TotalAmount,
    decimal DepositAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    decimal RefundedAmount,
    string PaymentStatus
);

public record AdminPaymentEntryDto(
    Guid PaymentId,
    string Kind,
    string Method,
    decimal Amount,
    string TransactionStatus,
    string? ProviderTransactionId,
    Guid? RelatedPaymentId,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt
);

public record AdminBookingStatusHistoryDto(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    Guid? ChangedByUserId,
    string? Reason,
    DateTimeOffset CreatedAt
);

public record AdminCheckInDetailDto(
    Guid Id,
    Guid StaffUserId,
    DateTimeOffset CheckedInAt,
    bool OutstandingPaymentOverride,
    string? OverrideReason
);

public record AdminCancellationRequestItemDto(
    Guid Id,
    string Status,
    string Reason,
    decimal CalculatedRefundAmount,
    decimal? ApprovedRefundAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt
);

public record AdminBookingReviewItemDto(
    Guid Id,
    int Rating,
    string? Comment,
    DateTimeOffset CreatedAt
);

public record AdminBookingDetailResponse(
    Guid BookingId,
    string BookingCode,
    string Source,
    string BookingStatus,
    string PaymentStatus,
    AdminCustomerSnapshotDto Customer,
    AdminFacilitySnapshotDto Facility,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    int DurationMinutes,
    List<AdminBookingSlotDetailDto> Slots,
    AdminFinancialSummaryDto FinancialSummary,
    List<AdminPaymentEntryDto> Payments,
    List<AdminBookingStatusHistoryDto> StatusHistories,
    AdminCheckInDetailDto? CheckIn,
    List<AdminCancellationRequestItemDto> CancellationRequests,
    AdminBookingReviewItemDto? Review,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt
);

public record AdminCancelBookingRequest(
    [Required]
    [MaxLength(500)]
    string Reason,
    [Range(0, double.MaxValue)]
    decimal? RefundAmount = null
);

public record AdminCancelBookingResponse(
    Guid BookingId,
    string BookingStatus,
    string PaymentStatus,
    decimal RefundAmount,
    Guid? RefundPaymentId,
    DateTimeOffset CancelledAt
);
