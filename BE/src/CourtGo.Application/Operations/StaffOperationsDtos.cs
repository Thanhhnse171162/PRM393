using CourtGo.Application.Bookings;
namespace CourtGo.Application.Operations;
public record StaffScheduleQuery(DateOnly? Date = null, string? Status = null, Guid? SportId = null, string? Search = null, int PageNumber = 1, int PageSize = 20);
public record StaffScheduleItem(Guid BookingId, string BookingCode, string CustomerName, string CustomerPhone, Guid CourtId, string CourtName, string SportName, DateTimeOffset StartAt, DateTimeOffset EndAt, string BookingStatus, string PaymentStatus, decimal TotalAmount);
public record PaymentEntryDto(Guid Id, string Kind, string Method, decimal Amount, string Status, DateTimeOffset? PaidAt);
public record CheckInEntryDto(Guid StaffUserId, DateTimeOffset CheckedInAt, bool OutstandingPaymentOverride, string? OverrideReason);
public record StaffBookingDetail(StaffScheduleItem Booking, BookingPaymentDto Payment, IReadOnlyList<BookingSlotDto> Slots, IReadOnlyList<PaymentEntryDto> Payments, CheckInEntryDto? CheckIn);
public record CourtStateDto(Guid CourtId, string Name, string SportName, string Status, string OperationalState);
public record StaffDashboardDto(Guid CenterId, DateOnly Date, int WaitingCheckIn, int Playing, int Upcoming, int NeedsAttention, IReadOnlyList<StaffScheduleItem> NextBookings, IReadOnlyList<CourtStateDto> CourtStatuses);
