using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Application.Operations;
using CourtGo.Domain.Enums;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace CourtGo.Infrastructure.Services;
public class DevelopmentRefundSimulationService(CourtGoDbContext db, IDevelopmentPaymentGateway gateway, ICancellationService service) : IDevelopmentRefundSimulationService
{
    public async Task<CancellationRequestDto> SimulateAsync(Guid paymentId, SimulatePaymentRequest request, CancellationToken ct)
    {
        var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == paymentId && p.PaymentKind == PaymentKind.Refund, ct)
            ?? throw new NotFoundException("Refund not found.");
        var verified = await gateway.VerifyRefundSimulationAsync(new(payment.Id, payment.BookingId, payment.PaymentMethod, payment.Amount),
            payment.ProviderTransactionId, request.Result, ct);
        return await service.ApplyVerifiedRefundAsync(verified, ct);
    }
}
