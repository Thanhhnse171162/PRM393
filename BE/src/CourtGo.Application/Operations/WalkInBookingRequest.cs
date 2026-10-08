using System.Text.Json.Serialization;
namespace CourtGo.Application.Operations;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record WalkInBookingRequest(string CustomerName, string CustomerPhone, Guid CourtId, List<DateTimeOffset>? SlotStartAts, string PaymentMethod = "Cash");
