using CourtGo.Domain.Enums;

namespace CourtGo.Application.Interfaces;

public record GatewayPaymentRequest(Guid PaymentId, Guid BookingId, PaymentMethod PaymentMethod, decimal Amount);
public record GatewayPaymentAttempt(string ProviderTransactionId, string? PaymentUrl, string Gateway);
/// <summary>Trusted result created ONLY after gateway verification, never bound to HTTP input.</summary>
public record VerifiedPaymentResult(Guid PaymentId, string ProviderTransactionId, decimal Amount,
    PaymentTransactionStatus TransactionStatus);

public interface IPaymentGateway
{
    string Name { get; }
    bool Supports(PaymentMethod method);
    // Implementations must use PaymentId as the provider idempotency key.
    Task<GatewayPaymentAttempt> CreateDepositAsync(GatewayPaymentRequest request, CancellationToken cancellationToken = default);
}

public interface IDevelopmentPaymentGateway : IPaymentGateway
{
    Task<VerifiedPaymentResult> VerifySimulationAsync(GatewayPaymentRequest payment, string? providerTransactionId,
        string? result, CancellationToken cancellationToken = default);
}
