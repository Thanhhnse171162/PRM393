using CourtGo.Application.AdminBookings;
using CourtGo.Application.Bookings;
using CourtGo.Application.Common;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Entities;
using CourtGo.Domain.Enums;
using CourtGo.Domain.Rules;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

public class AdminBookingService(
    CourtGoDbContext db,
    BookingCommandExecutor commands,
    TimeProvider clock
) : IAdminBookingService
{
    public async Task<PagedResult<AdminBookingListItemResponse>> GetBookingsAsync(
        AdminBookingQuery query, CancellationToken cancellationToken = default)
    {
        StaffOperationsService.ValidatePage(query.PageNumber, query.PageSize);
        var bookings = db.Bookings.AsNoTracking();

        if (query.CenterId is Guid centerId)
        {
            bookings = bookings.Where(b => b.Court!.SportCenterId == centerId);
        }

        if (query.SportId is Guid sportId)
        {
            bookings = bookings.Where(b => b.Court!.SportId == sportId);
        }

        if (query.CourtId is Guid courtId)
        {
            bookings = bookings.Where(b => b.CourtId == courtId);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<BookingStatus>(query.Status, true, out var status) || !Enum.IsDefined(status))
                throw new ValidationException("Invalid booking status.");
            bookings = bookings.Where(b => b.BookingStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(query.PaymentStatus))
        {
            if (!Enum.TryParse<BookingPaymentStatus>(query.PaymentStatus, true, out var paymentStatus) || !Enum.IsDefined(paymentStatus))
                throw new ValidationException("Invalid booking payment status.");
            bookings = bookings.Where(b => b.PaymentStatus == paymentStatus);
        }

        if (query.DateFrom is DateTimeOffset dateFrom)
        {
            bookings = bookings.Where(b => b.StartAt >= dateFrom);
        }

        if (query.DateTo is DateTimeOffset dateTo)
        {
            bookings = bookings.Where(b => b.StartAt <= dateTo);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (search.Length > 200)
                throw new ValidationException("Search exceeds 200 characters.");
            bookings = bookings.Where(b =>
                b.BookingCode.Contains(search) ||
                b.CustomerNameSnapshot.Contains(search) ||
                b.CustomerPhoneSnapshot.Contains(search));
        }

        var totalItems = await bookings.CountAsync(cancellationToken);

        var rows = await bookings
            .OrderByDescending(b => b.CreatedAt)
            .ThenByDescending(b => b.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(b => new
            {
                b.Id,
                b.BookingCode,
                b.CustomerNameSnapshot,
                b.CustomerPhoneSnapshot,
                SportCenterId = b.Court != null ? b.Court.SportCenterId : Guid.Empty,
                b.CenterNameSnapshot,
                b.CourtId,
                b.CourtNameSnapshot,
                SportId = b.Court != null ? b.Court.SportId : Guid.Empty,
                b.SportNameSnapshot,
                b.StartAt,
                b.EndAt,
                b.TotalAmount,
                PaidAmount = db.Payments.Where(PaymentRules.SuccessfulCharge).Where(p => p.BookingId == b.Id).Sum(p => (decimal?)p.Amount) ?? 0m,
                b.BookingStatus,
                b.PaymentStatus,
                b.Source,
                b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new AdminBookingListItemResponse(
            r.Id,
            r.BookingCode,
            r.CustomerNameSnapshot,
            r.CustomerPhoneSnapshot,
            r.SportCenterId,
            r.CenterNameSnapshot,
            r.CourtId,
            r.CourtNameSnapshot,
            r.SportId,
            r.SportNameSnapshot,
            r.StartAt,
            r.EndAt,
            r.TotalAmount,
            r.PaidAmount,
            PaymentRules.CalculateRemaining(r.TotalAmount, r.PaidAmount),
            r.BookingStatus.ToString(),
            r.PaymentStatus.ToString(),
            r.Source.ToString(),
            r.CreatedAt
        )).ToList();

        return new(items, query.PageNumber, query.PageSize, totalItems, (int)Math.Ceiling(totalItems / (double)query.PageSize));
    }

    public async Task<AdminBookingDetailResponse> GetBookingDetailAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await db.Bookings.AsNoTracking()
            .Where(b => b.Id == bookingId)
            .Select(b => new
            {
                b.Id,
                b.BookingCode,
                b.Source,
                b.BookingStatus,
                b.PaymentStatus,
                b.CustomerUserId,
                b.CustomerNameSnapshot,
                b.CustomerPhoneSnapshot,
                b.CustomerEmailSnapshot,
                SportCenterId = b.Court != null ? b.Court.SportCenterId : Guid.Empty,
                b.CenterNameSnapshot,
                b.CourtId,
                b.CourtNameSnapshot,
                SportId = b.Court != null ? b.Court.SportId : Guid.Empty,
                b.SportNameSnapshot,
                b.StartAt,
                b.EndAt,
                b.DurationMinutes,
                b.TotalAmount,
                b.DepositAmount,
                b.CreatedByUserId,
                b.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Booking not found.", ErrorCodes.BookingNotFound);

        var slots = await db.BookingSlots.AsNoTracking()
            .Where(s => s.BookingId == bookingId)
            .OrderBy(s => s.StartAt)
            .Select(s => new AdminBookingSlotDetailDto(
                s.Id,
                s.StartAt,
                s.EndAt,
                s.UnitPrice,
                s.ReservationState.ToString()
            ))
            .ToListAsync(cancellationToken);

        var payments = await db.Payments.AsNoTracking()
            .Where(p => p.BookingId == bookingId)
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Select(p => new AdminPaymentEntryDto(
                p.Id,
                p.PaymentKind.ToString(),
                p.PaymentMethod.ToString(),
                p.Amount,
                p.TransactionStatus.ToString(),
                p.ProviderTransactionId,
                p.RelatedPaymentId,
                p.PaidAt,
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        var paidAmount = payments
            .Where(p => (p.Kind == PaymentKind.Deposit.ToString() || p.Kind == PaymentKind.Remaining.ToString()) &&
                        p.TransactionStatus == PaymentTransactionStatus.Succeeded.ToString())
            .Sum(p => p.Amount);

        var refundedAmount = payments
            .Where(p => p.Kind == PaymentKind.Refund.ToString() &&
                        p.TransactionStatus == PaymentTransactionStatus.Succeeded.ToString())
            .Sum(p => p.Amount);

        var statusHistories = await db.BookingStatusHistories.AsNoTracking()
            .Where(h => h.BookingId == bookingId)
            .OrderBy(h => h.CreatedAt)
            .Select(h => new AdminBookingStatusHistoryDto(
                h.Id,
                h.FromStatus != null ? h.FromStatus.ToString() : null,
                h.ToStatus.ToString(),
                h.ChangedByUserId,
                h.Reason,
                h.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        var checkIn = await db.CheckIns.AsNoTracking()
            .Where(c => c.BookingId == bookingId)
            .Select(c => new AdminCheckInDetailDto(
                c.Id,
                c.StaffUserId,
                c.CheckedInAt,
                c.OutstandingPaymentOverride,
                c.OverrideReason
            ))
            .SingleOrDefaultAsync(cancellationToken);

        var cancellationRequests = await db.CancellationRequests.AsNoTracking()
            .Where(r => r.BookingId == bookingId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new AdminCancellationRequestItemDto(
                r.Id,
                r.Status.ToString(),
                r.Reason,
                r.CalculatedRefundAmount,
                r.ApprovedRefundAmount,
                r.CreatedAt,
                r.ProcessedAt
            ))
            .ToListAsync(cancellationToken);

        var review = await db.Reviews.AsNoTracking()
            .Where(r => r.BookingId == bookingId)
            .Select(r => new AdminBookingReviewItemDto(
                r.Id,
                r.Rating,
                r.Comment,
                r.CreatedAt
            ))
            .SingleOrDefaultAsync(cancellationToken);

        return new AdminBookingDetailResponse(
            booking.Id,
            booking.BookingCode,
            booking.Source.ToString(),
            booking.BookingStatus.ToString(),
            booking.PaymentStatus.ToString(),
            new AdminCustomerSnapshotDto(
                booking.CustomerUserId,
                booking.CustomerNameSnapshot,
                booking.CustomerPhoneSnapshot,
                booking.CustomerEmailSnapshot
            ),
            new AdminFacilitySnapshotDto(
                booking.SportCenterId,
                booking.CenterNameSnapshot,
                booking.CourtId,
                booking.CourtNameSnapshot,
                booking.SportId,
                booking.SportNameSnapshot
            ),
            booking.StartAt,
            booking.EndAt,
            booking.DurationMinutes,
            slots,
            new AdminFinancialSummaryDto(
                booking.TotalAmount,
                booking.DepositAmount,
                paidAmount,
                PaymentRules.CalculateRemaining(booking.TotalAmount, paidAmount),
                refundedAmount,
                booking.PaymentStatus.ToString()
            ),
            payments,
            statusHistories,
            checkIn,
            cancellationRequests,
            review,
            booking.CreatedByUserId,
            booking.CreatedAt
        );
    }

    public Task<AdminCancelBookingResponse> CancelBookingAsync(
        Guid adminUserId, Guid bookingId, AdminCancelBookingRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500)
            throw new ValidationException("Reason is required, maximum 500 characters.");

        return commands.ExecuteAsync(bookingId, async (booking, token) =>
        {
            if (booking.BookingStatus is BookingStatus.Completed or BookingStatus.Cancelled or BookingStatus.Expired or BookingStatus.NoShow)
                throw new ConflictException($"Cannot cancel a booking in status '{booking.BookingStatus}'.");

            var paid = await BookingPaymentQueries.GetPaidAmountAsync(db, booking.Id, token);
            var refunded = await db.Payments.Where(p => p.BookingId == booking.Id && p.PaymentKind == PaymentKind.Refund &&
                (p.TransactionStatus == PaymentTransactionStatus.Succeeded || p.TransactionStatus == PaymentTransactionStatus.Pending))
                .SumAsync(p => (decimal?)p.Amount, token) ?? 0m;
            var refundable = Math.Max(0m, paid - refunded);

            if (request.RefundAmount is decimal requestedRefund)
            {
                if (requestedRefund < 0)
                    throw new ValidationException("Refund amount must be non-negative.");
                if (requestedRefund > refundable)
                    throw new ValidationException("Refund amount cannot exceed actual refundable charges.");
            }

            var approvedRefund = request.RefundAmount ?? refundable;
            var now = clock.GetUtcNow();

            var slots = await commands.ReloadSlotsAsync(booking.Id, token);
            foreach (var slot in slots)
            {
                slot.ReservationState = ReservationState.Released;
                slot.IsOccupying = false;
                slot.HoldExpiresAt = null;
            }

            db.BookingStatusHistories.Add(new BookingStatusHistory
            {
                BookingId = booking.Id,
                FromStatus = booking.BookingStatus,
                ToStatus = BookingStatus.Cancelled,
                ChangedByUserId = adminUserId,
                Reason = request.Reason.Trim(),
                CreatedAt = now
            });

            Payment? refundPayment = null;
            if (approvedRefund > 0)
            {
                var originalPayment = await db.Payments.AsNoTracking()
                    .Where(p => p.BookingId == booking.Id && (p.PaymentKind == PaymentKind.Deposit || p.PaymentKind == PaymentKind.Remaining) && p.TransactionStatus == PaymentTransactionStatus.Succeeded)
                    .OrderBy(p => p.CreatedAt)
                    .FirstOrDefaultAsync(token);

                var method = originalPayment?.PaymentMethod ?? PaymentMethod.MoMo;
                refundPayment = new Payment
                {
                    BookingId = booking.Id,
                    PaymentKind = PaymentKind.Refund,
                    PaymentMethod = method,
                    Amount = approvedRefund,
                    TransactionStatus = PaymentTransactionStatus.Pending,
                    RelatedPaymentId = originalPayment?.Id,
                    PaidByUserId = adminUserId,
                    CreatedAt = now,
                    ProviderTransactionId = $"dev-{method}-{Guid.NewGuid():N}"
                };
                db.Payments.Add(refundPayment);
                booking.PaymentStatus = BookingPaymentStatus.RefundPending;
            }
            else if (paid == 0)
            {
                booking.PaymentStatus = BookingPaymentStatus.Unpaid;
            }

            booking.BookingStatus = BookingStatus.Cancelled;

            if (booking.CustomerUserId is Guid customerId)
            {
                var refundText = approvedRefund > 0 ? $" Số tiền hoàn: {approvedRefund:N0} đ." : "";
                db.Notifications.Add(new Notification
                {
                    UserId = customerId,
                    Type = NotificationType.Booking,
                    Title = "Lịch đặt sân đã bị hủy bởi Quản trị viên",
                    Message = $"Lịch đặt {booking.BookingCode} đã bị hủy bởi quản trị viên. Lý do: {request.Reason.Trim()}.{refundText}",
                    ReferenceType = "Booking",
                    ReferenceId = booking.Id,
                    CreatedAt = now
                });
            }

            return new AdminCancelBookingResponse(
                booking.Id,
                booking.BookingStatus.ToString(),
                booking.PaymentStatus.ToString(),
                approvedRefund,
                refundPayment?.Id,
                now
            );
        }, cancellationToken);
    }
}
