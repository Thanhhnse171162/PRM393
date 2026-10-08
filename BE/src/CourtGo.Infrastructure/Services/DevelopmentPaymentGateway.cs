using CourtGo.Application.Common.Exceptions;
using CourtGo.Application.Interfaces;
using CourtGo.Domain.Enums;

namespace CourtGo.Infrastructure.Services;

/// <summary>Registered only in Development. No real money or provider credentials are involved.</summary>
public sealed class DevelopmentPaymentGateway : IDevelopmentPaymentGateway
{
    public string Name => "Development";
    public bool Supports(PaymentMethod method) => method is PaymentMethod.MoMo or PaymentMethod.VNPay;

    public Task<GatewayPaymentAttempt> CreateDepositAsync(GatewayPaymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Supports(request.PaymentMethod))
            throw new ValidationException("This development gateway simulates only MoMo and VNPay.", ErrorCodes.UnsupportedPaymentMethod);
        return Task.FromResult(new GatewayPaymentAttempt(Reference(request), null, Name));
    }

    public Task<VerifiedPaymentResult> VerifySimulationAsync(GatewayPaymentRequest payment, string? providerTransactionId,
        string? result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Supports(payment.PaymentMethod) || !string.Equals(providerTransactionId, Reference(payment), StringComparison.Ordinal))
            throw new ValidationException("Development payment reference could not be verified.", ErrorCodes.PaymentVerificationFailed);
        var status = result?.ToLowerInvariant() switch
        {
            "success" => PaymentTransactionStatus.Succeeded,
            "failed" => PaymentTransactionStatus.Failed,
            "cancelled" => PaymentTransactionStatus.Cancelled,
            _ => throw new ValidationException("result must be success, failed or cancelled.", ErrorCodes.PaymentVerificationFailed)
        };
        return Task.FromResult(new VerifiedPaymentResult(payment.PaymentId, providerTransactionId!, payment.Amount, status));
    }

    private static string Reference(GatewayPaymentRequest request) => $"dev-{request.PaymentMethod}-{request.PaymentId:N}";
}

/// <summary>Fail closed outside Development until a real verified provider adapter is configured.</summary>
public sealed class UnavailablePaymentGateway : IPaymentGateway
{
    public string Name => "Unavailable";
    public bool Supports(PaymentMethod method) => false;
    public Task<GatewayPaymentAttempt> CreateDepositAsync(GatewayPaymentRequest request, CancellationToken cancellationToken = default)
        => throw new ConfigurationException("No production payment gateway has been configured.", ErrorCodes.PaymentGatewayNotConfigured);
}
