using CourtGo.Application.Bookings;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using CourtGo.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CourtGo.Infrastructure.Services;

public sealed class PaymentReconciliationService(CourtGoDbContext db, IDepositPaymentService deposits,
    VnPayTransactionQuery query, PaymentReconciliationGate gate, ILogger<PaymentReconciliationService> logger)
    : IPaymentReconciliationService
{
    public async Task<BookingPaymentStatusResponse> ReconcileAsync(Guid customerUserId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        // Authorize before any provider request; never accept a reference or amount from the customer.
        await deposits.GetPaymentStatusAsync(customerUserId, bookingId, cancellationToken);
        var mutex = gate.For(bookingId);
        await mutex.WaitAsync(cancellationToken);
        try
        {
            var payment = await db.Payments.AsNoTracking().Where(p => p.BookingId == bookingId &&
                p.PaymentKind == PaymentKind.Deposit && p.PaymentMethod == PaymentMethod.VNPay &&
                p.TransactionStatus == PaymentTransactionStatus.Pending && p.CheckoutUrl != null)
                .OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync(cancellationToken);
            if (payment != null && gate.TryBegin(payment.Id))
            {
                var result = await query.QueryAsync(payment, cancellationToken);
                logger.LogInformation("VNPAY QueryDR verified PaymentId={PaymentId} BookingId={BookingId} Status={Status} Receipt={Receipt}",
                    payment.Id, bookingId, result.TransactionStatus, result.ProviderReceiptId);
                if (result.TransactionStatus != PaymentTransactionStatus.Pending)
                    await deposits.ApplyVerifiedResultAsync(result, customerUserId, cancellationToken);
            }
            return await deposits.GetPaymentStatusAsync(customerUserId, bookingId, cancellationToken);
        }
        finally { mutex.Release(); }
    }
}
