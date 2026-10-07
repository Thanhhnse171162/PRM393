using CourtGo.Domain.Enums;

namespace CourtGo.Domain.Entities;

public class CheckIn
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public Guid StaffUserId { get; set; }
    public User? StaffUser { get; set; }

    public CheckInMethod Method { get; set; } = CheckInMethod.QR;
    public DateTimeOffset CheckedInAt { get; set; } = DateTimeOffset.UtcNow;

    public bool OutstandingPaymentOverride { get; set; }
    public string? OverrideReason { get; set; }
    public string? Note { get; set; }
}
