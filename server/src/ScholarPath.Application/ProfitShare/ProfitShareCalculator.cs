using System;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ProfitShare;

/// <summary>
/// Pure profit-share math (PB-014). Works entirely in integer cents so the
/// platform share and the payee share always re-sum to the exact gross amount.
/// </summary>
public static class ProfitShareCalculator
{
    /// <summary>
    /// Fallback rate, used only when no financial rule / profit-share config is
    /// in force. FR-204: the default portal profit-share is 10% for every
    /// payment type unless an admin overrides it.
    /// </summary>
    public static decimal DefaultPercentage(PaymentType type) => type switch
    {
        PaymentType.CompanyReview => 0.10m,
        _ => 0.10m,
    };

    public static ProfitShareBreakdown Calculate(long grossAmountCents, decimal percentage)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(grossAmountCents);
        if (percentage is < 0m or > 1m)
            throw new ArgumentOutOfRangeException(
                nameof(percentage), percentage, "Percentage must be between 0 and 1.");

        var profitShareCents = (long)Math.Round(
            grossAmountCents * percentage, MidpointRounding.AwayFromZero);

        // Payee takes the remainder so the split always re-sums to gross exactly.
        var payeeAmountCents = grossAmountCents - profitShareCents;

        return new ProfitShareBreakdown(
            grossAmountCents, profitShareCents, payeeAmountCents, percentage);
    }
}

public sealed record ProfitShareBreakdown(
    long GrossAmountCents,
    long ProfitShareAmountCents,
    long PayeeAmountCents,
    decimal Percentage);
