using System;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.FinancialConfig;

/// <summary>
/// Pure financial-rule math (FR-167/175). Works in integer cents so the platform
/// take and the payee net always re-sum to the exact gross amount. A rule's
/// platform fee is either a percentage of the gross or a flat cent amount; the
/// profit-share is always a percentage of the gross.
/// </summary>
public static class FinancialCalculator
{
    public static FinancialBreakdown Calculate(long grossAmountCents, FinancialConfigRule rule) =>
        Calculate(grossAmountCents, rule.FeeKind, rule.FeePercentage,
            rule.FeeAmountCents, rule.ProfitSharePercentage);

    public static FinancialBreakdown Calculate(
        long grossAmountCents,
        FeeKind feeKind,
        decimal? feePercentage,
        long? feeAmountCents,
        decimal profitSharePercentage)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(grossAmountCents);

        var feeCents = feeKind switch
        {
            FeeKind.Percentage => (long)Math.Round(
                grossAmountCents * (feePercentage ?? 0m), MidpointRounding.AwayFromZero),
            FeeKind.FixedAmount => feeAmountCents ?? 0L,
            _ => 0L,
        };

        var profitShareCents = (long)Math.Round(
            grossAmountCents * profitSharePercentage, MidpointRounding.AwayFromZero);

        var platformTotalCents = feeCents + profitShareCents;

        // Payee takes the remainder so the split always re-sums to gross exactly.
        var payeeNetCents = grossAmountCents - platformTotalCents;

        var effectiveFeeRate = grossAmountCents == 0
            ? 0m
            : (decimal)feeCents / grossAmountCents;

        return new FinancialBreakdown(
            grossAmountCents, feeCents, profitShareCents,
            platformTotalCents, payeeNetCents, effectiveFeeRate);
    }
}

public sealed record FinancialBreakdown(
    long GrossAmountCents,
    long FeeCents,
    long ProfitShareCents,
    long PlatformTotalCents,
    long PayeeNetCents,
    decimal EffectiveFeeRate);
