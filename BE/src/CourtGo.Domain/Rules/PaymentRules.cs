namespace CourtGo.Domain.Rules;

public static class PaymentRules
{
    /// <summary>Default deposit rate (30%). Will become a system configuration value.</summary>
    public const decimal DefaultDepositRate = 0.30m;

    /// <summary>Deposit rounded to whole VND. 240,000 -> 72,000.</summary>
    public static decimal CalculateDeposit(decimal total, decimal rate = DefaultDepositRate) =>
        Math.Round(total * rate, 0, MidpointRounding.AwayFromZero);

    /// <summary>Amount still owed at the branch. 240,000 - 72,000 = 168,000.</summary>
    public static decimal CalculateRemaining(decimal total, decimal deposit) =>
        Math.Max(0, total - deposit);
}
