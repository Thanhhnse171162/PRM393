using System.Text.Json.Serialization;

namespace CourtGo.Application.Availability;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AvailabilitySlotStatus
{
    Available,
    Booked,
    Held,
    Blocked
}
