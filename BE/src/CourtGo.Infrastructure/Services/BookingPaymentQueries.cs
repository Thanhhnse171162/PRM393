using CourtGo.Domain.Entities;
using CourtGo.Domain.Rules;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

internal static class BookingPaymentQueries
{
    public static Task<decimal> GetPaidAmountAsync(CourtGoDbContext dbContext, Guid bookingId, CancellationToken cancellationToken)
        => dbContext.Payments.AsNoTracking().Where(PaymentRules.SuccessfulCharge)
            .Where(p => p.BookingId == bookingId).SumAsync(p => p.Amount, cancellationToken);
}
