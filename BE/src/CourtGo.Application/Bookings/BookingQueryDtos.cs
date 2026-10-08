namespace CourtGo.Application.Bookings;

public record BookingHistoryQuery(string? StatusGroup = "upcoming", int PageNumber = 1, int PageSize = 20);
public record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalItems, int TotalPages);
public record BookingListItemDto(
    Guid BookingId, string BookingCode, string BookingStatus, string PaymentStatus,
    string CourtName, string CenterName, string SportName,
    DateTimeOffset StartAt, DateTimeOffset EndAt, int DurationMinutes,
    decimal TotalAmount, decimal DepositAmount, decimal PaidAmount, decimal RemainingAmount,
    bool HasQr, DateTimeOffset? HoldExpiresAt);
public record BookingCustomerDto(string FullName, string PhoneNumber);
public record BookingCenterDto(string Name);
public record BookingCourtDto(Guid Id, string Name);
public record BookingSportDto(string Name);
public record BookingSlotDto(DateTimeOffset StartAt, DateTimeOffset EndAt, decimal Price);
public record BookingPaymentDto(decimal TotalAmount, decimal DepositAmount, decimal PaidAmount, decimal RemainingAmount);
public record BookingDetailDto(
    Guid BookingId, string BookingCode, string BookingStatus, string PaymentStatus,
    BookingCustomerDto Customer, BookingCenterDto Center, BookingCourtDto Court, BookingSportDto Sport,
    DateTimeOffset StartAt, DateTimeOffset EndAt, int DurationMinutes,
    IReadOnlyList<BookingSlotDto> Slots, BookingPaymentDto Payment, bool HasQr, DateTimeOffset CreatedAt);
public record BookingQrResponse(
    Guid BookingId, string BookingCode, string QrValue, string CourtName, string CenterName,
    DateTimeOffset StartAt, DateTimeOffset EndAt);
