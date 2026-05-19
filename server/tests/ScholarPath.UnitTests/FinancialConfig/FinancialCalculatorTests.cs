using FluentAssertions;
using ScholarPath.Application.FinancialConfig;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using Xunit;

namespace ScholarPath.UnitTests.FinancialConfig;

public class FinancialCalculatorTests
{
    [Theory]
    // gross, feePct, sharePct, expFee, expShare, expPayee
    [InlineData(10000, 0.05, 0.10, 500, 1000, 8500)]
    [InlineData(10000, 0.00, 0.10, 0, 1000, 9000)]
    [InlineData(5000, 0.10, 0.20, 500, 1000, 3500)]
    public void Calculate_percentage_fee_splits_gross(
        long gross, double feePct, double sharePct,
        long expFee, long expShare, long expPayee)
    {
        var b = FinancialCalculator.Calculate(
            gross, FeeKind.Percentage, (decimal)feePct, null, (decimal)sharePct);

        b.FeeCents.Should().Be(expFee);
        b.ProfitShareCents.Should().Be(expShare);
        b.PayeeNetCents.Should().Be(expPayee);
        b.PlatformTotalCents.Should().Be(expFee + expShare);
    }

    [Theory]
    // gross, feeAmount, sharePct, expFee, expShare, expPayee
    [InlineData(10000, 300, 0.10, 300, 1000, 8700)]
    [InlineData(5000, 0, 0.10, 0, 500, 4500)]
    public void Calculate_fixed_fee_splits_gross(
        long gross, long feeAmount, double sharePct,
        long expFee, long expShare, long expPayee)
    {
        var b = FinancialCalculator.Calculate(
            gross, FeeKind.FixedAmount, null, feeAmount, (decimal)sharePct);

        b.FeeCents.Should().Be(expFee);
        b.ProfitShareCents.Should().Be(expShare);
        b.PayeeNetCents.Should().Be(expPayee);
    }

    [Theory]
    [InlineData(333, 0.10, 0.15)]
    [InlineData(99999, 0.07, 0.13)]
    [InlineData(101, 0.50, 0.25)]
    public void Calculate_parts_always_resum_to_gross(long gross, double feePct, double sharePct)
    {
        var b = FinancialCalculator.Calculate(
            gross, FeeKind.Percentage, (decimal)feePct, null, (decimal)sharePct);

        (b.FeeCents + b.ProfitShareCents + b.PayeeNetCents).Should().Be(gross);
        b.PlatformTotalCents.Should().Be(b.FeeCents + b.ProfitShareCents);
    }

    [Fact]
    public void Calculate_rounds_half_away_from_zero()
    {
        // 101 * 0.50 = 50.5 -> 51
        var b = FinancialCalculator.Calculate(101, FeeKind.Percentage, 0.50m, null, 0m);

        b.FeeCents.Should().Be(51);
    }

    [Fact]
    public void Calculate_effective_fee_rate_reflects_fixed_fee()
    {
        var b = FinancialCalculator.Calculate(10000, FeeKind.FixedAmount, null, 300, 0.10m);

        b.EffectiveFeeRate.Should().Be(0.03m);
    }

    [Fact]
    public void Calculate_allows_negative_payee_net_for_an_unviable_rule()
    {
        // fee 60% + profit-share 60% = 120% of gross -> the payee goes negative.
        var b = FinancialCalculator.Calculate(10000, FeeKind.Percentage, 0.60m, null, 0.60m);

        b.PlatformTotalCents.Should().Be(12000);
        b.PayeeNetCents.Should().Be(-2000);
    }

    [Fact]
    public void Calculate_rejects_negative_gross()
    {
        var act = () => FinancialCalculator.Calculate(-1, FeeKind.Percentage, 0.10m, null, 0.10m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Calculate_from_rule_uses_rule_fields()
    {
        var rule = new FinancialConfigRule
        {
            PaymentType = PaymentType.ConsultantBooking,
            FeeKind = FeeKind.FixedAmount,
            FeeAmountCents = 250,
            ProfitSharePercentage = 0.10m,
        };

        var b = FinancialCalculator.Calculate(10000, rule);

        b.FeeCents.Should().Be(250);
        b.ProfitShareCents.Should().Be(1000);
        b.PayeeNetCents.Should().Be(8750);
    }
}
