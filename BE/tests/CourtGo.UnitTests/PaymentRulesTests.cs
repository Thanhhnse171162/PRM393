using CourtGo.Domain.Rules;

namespace CourtGo.UnitTests;

public class PaymentRulesTests
{
    [Fact]
    public void Deposit_Of_240000_Is_72000_And_Remaining_168000()
    {
        var deposit = PaymentRules.CalculateDeposit(240_000m);
        var remaining = PaymentRules.CalculateRemaining(240_000m, deposit);

        Assert.Equal(72_000m, deposit);
        Assert.Equal(168_000m, remaining);
    }

    [Fact]
    public void Remaining_NeverNegative()
    {
        Assert.Equal(0m, PaymentRules.CalculateRemaining(100m, 150m));
    }
}
