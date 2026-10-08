using CourtGo.Application.Bookings;
using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CourtGo.Infrastructure.Services;

// Registered only in Development alongside IDevelopmentPaymentGateway.
public sealed class DevelopmentPaymentSimulationService(CourtGoDbContext dbContext,
    IDevelopmentPaymentGateway gateway, IDepositPaymentService depositPaymentService) : IDevelopmentPaymentSimulationService
{
    private readonly CourtGoDbContext _dbContext = dbContext;
    private readonly IDevelopmentPaymentGateway _gateway = gateway;
    private readonly IDepositPaymentService _depositPaymentService = depositPaymentService;

    public async Task<DepositConfirmationResponse> SimulateAsync(Guid customerUserId, Guid paymentId,
        SimulatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.Payments.AsNoTracking()
            .Where(p => p.Id == paymentId && p.Booking!.CustomerUserId == customerUserId)
            .Select(p => new { p.Id, p.BookingId, p.PaymentMethod, p.Amount, p.ProviderTransactionId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Payment was not found.", ErrorCodes.PaymentNotFound);
        var verified = await _gateway.VerifySimulationAsync(
            new(payment.Id, payment.BookingId, payment.PaymentMethod, payment.Amount),
            payment.ProviderTransactionId, request.Result, cancellationToken);
        return await _depositPaymentService.ApplyVerifiedResultAsync(verified, customerUserId, cancellationToken);
    }
}
