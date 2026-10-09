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

    public Task<GatewayPaymentAttempt> CreateRefundAsync(GatewayPaymentRequest request, Guid originalPaymentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new GatewayPaymentAttempt($"dev-refund-{request.PaymentMethod}-{request.PaymentId:N}", null, Name));
    }
    public Task<VerifiedPaymentResult> VerifyRefundSimulationAsync(GatewayPaymentRequest payment, string? providerTransactionId,
        string? result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (providerTransactionId != $"dev-refund-{payment.PaymentMethod}-{payment.PaymentId:N}")
            throw new ValidationException("Development refund reference is invalid.", ErrorCodes.PaymentVerificationFailed);
        if (!string.Equals(result, "success", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Development refunds currently support result=success only.");
        return Task.FromResult(new VerifiedPaymentResult(payment.PaymentId, providerTransactionId!, payment.Amount, PaymentTransactionStatus.Succeeded));
    }
}

/// <summary>Fail closed outside Development until a real verified provider adapter is configured.</summary>
public sealed class UnavailablePaymentGateway : IPaymentGateway
{
    public void ValidateConfiguration(PaymentMethod method) => throw new ConfigurationException("No payment gateway is configured.", ErrorCodes.PaymentGatewayNotConfigured);
    public string Name => "Unavailable";
    public bool Supports(PaymentMethod method) => false;
    public Task<GatewayPaymentAttempt> CreateDepositAsync(GatewayPaymentRequest request, CancellationToken cancellationToken = default)
        => throw new ConfigurationException("No production payment gateway has been configured.", ErrorCodes.PaymentGatewayNotConfigured);
}
