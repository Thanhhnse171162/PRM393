using System.Text.Json.Serialization;
namespace CourtGo.Application.Operations;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CreateCancellationRequest(string Reason);
public record CancellationRequestDto(Guid Id, Guid BookingId, string Status, string Reason, decimal CalculatedRefundAmount,
    decimal? ApprovedRefundAmount, DateTimeOffset CreatedAt, DateTimeOffset? ProcessedAt);
