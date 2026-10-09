using CourtGo.Domain.Enums;

namespace CourtGo.Application.Interfaces;

public record GatewayPaymentRequest(Guid PaymentId, Guid BookingId, PaymentMethod PaymentMethod, decimal Amount,
    DateTimeOffset? CreatedAt = null, DateTimeOffset? ExpiresAt = null);
public record GatewayPaymentAttempt(string ProviderTransactionId, string? PaymentUrl, string Gateway);
/// <summary>Trusted result created ONLY after gateway verification, never bound to HTTP input.</summary>
public record VerifiedPaymentResult(Guid PaymentId, string ProviderTransactionId, decimal Amount,
    PaymentTransactionStatus TransactionStatus, string? ProviderReceiptId = null, string? Gateway = null);

public interface IPaymentGateway
{
    string Name { get; }
    bool Supports(PaymentMethod method);
    void ValidateConfiguration(PaymentMethod method) { }
    string? GetReference(GatewayPaymentRequest request) => null;
    // Implementations must use PaymentId as the provider idempotency key.
    Task<GatewayPaymentAttempt> CreateDepositAsync(GatewayPaymentRequest request, CancellationToken cancellationToken = default);
    Task<GatewayPaymentAttempt> CreateRefundAsync(GatewayPaymentRequest request, Guid originalPaymentId, CancellationToken cancellationToken = default)
        => throw new CourtGo.Application.Common.Exceptions.ConfigurationException("No refund gateway is configured.",
            CourtGo.Application.Common.Exceptions.ErrorCodes.PaymentGatewayNotConfigured);
}

public interface IDevelopmentPaymentGateway : IPaymentGateway
{
    Task<VerifiedPaymentResult> VerifySimulationAsync(GatewayPaymentRequest payment, string? providerTransactionId,
        string? result, CancellationToken cancellationToken = default);
    Task<VerifiedPaymentResult> VerifyRefundSimulationAsync(GatewayPaymentRequest payment, string? providerTransactionId,
        string? result, CancellationToken cancellationToken = default)
        => throw new CourtGo.Application.Common.Exceptions.ConfigurationException("No development refund gateway is configured.");
}
