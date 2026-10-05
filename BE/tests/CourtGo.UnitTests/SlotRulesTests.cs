using CourtGo.Domain.Rules;

namespace CourtGo.UnitTests;

public class SlotRulesTests
{
    private static DateTime At(int hour, int minute = 0) => new(2026, 10, 6, hour, minute, 0);

    [Fact]
    public void ConsecutiveHourlySlots_AreValid()
    {
        Assert.True(SlotRules.AreConsecutive(new[] { At(18), At(17), At(19) }));
    }

    [Fact]
    public void SingleSlot_IsValid()
    {
        Assert.True(SlotRules.AreConsecutive(new[] { At(17) }));
    }

    [Fact]
    public void SlotsWithGap_AreInvalid()
    {
        Assert.False(SlotRules.AreConsecutive(new[] { At(17), At(19) }));
    }

    [Fact]
    public void DuplicateSlots_AreInvalid()
    {
        Assert.False(SlotRules.AreConsecutive(new[] { At(17), At(17) }));
    }

    [Fact]
    public void SlotNotAlignedToHour_IsInvalid()
    {
        Assert.False(SlotRules.AreConsecutive(new[] { At(17, 30) }));
    }

    [Fact]
    public void EmptySelection_IsInvalid()
    {
        Assert.False(SlotRules.AreConsecutive(Array.Empty<DateTime>()));
    }
}
