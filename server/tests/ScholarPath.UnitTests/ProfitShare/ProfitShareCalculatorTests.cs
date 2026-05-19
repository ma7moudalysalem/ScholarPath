using FluentAssertions;
using ScholarPath.Application.ProfitShare;
using ScholarPath.Domain.Enums;
using Xunit;

namespace ScholarPath.UnitTests.ProfitShare;

public class ProfitShareCalculatorTests
{
    [Theory]
    [InlineData(5000, 0.10, 500, 4500)]
    [InlineData(5000, 0.15, 750, 4250)]
    [InlineData(10000, 0.10, 1000, 9000)]
    [InlineData(100, 0.0, 0, 100)]
    [InlineData(100, 1.0, 100, 0)]
    public void Calculate_splits_amount_correctly(
        long gross, double pct, long expectedProfit, long expectedPayee)
    {
        var result = ProfitShareCalculator.Calculate(gross, (decimal)pct);

        result.ProfitShareAmountCents.Should().Be(expectedProfit);
        result.PayeeAmountCents.Should().Be(expectedPayee);
    }

    [Theory]
    [InlineData(101, 0.50)]
    [InlineData(333, 0.10)]
    [InlineData(99999, 0.15)]
    public void Calculate_halves_always_resum_to_gross(long gross, double pct)
    {
        var result = ProfitShareCalculator.Calculate(gross, (decimal)pct);

        (result.ProfitShareAmountCents + result.PayeeAmountCents).Should().Be(gross);
    }

    [Fact]
    public void Calculate_rounds_half_away_from_zero()
    {
        // 101 * 0.50 = 50.5 -> 51
        var result = ProfitShareCalculator.Calculate(101, 0.50m);

        result.ProfitShareAmountCents.Should().Be(51);
        result.PayeeAmountCents.Should().Be(50);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Calculate_rejects_out_of_range_percentage(double pct)
    {
        var act = () => ProfitShareCalculator.Calculate(5000, (decimal)pct);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Calculate_rejects_negative_gross()
    {
        var act = () => ProfitShareCalculator.Calculate(-1, 0.10m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(PaymentType.ConsultantBooking, 0.10)]
    [InlineData(PaymentType.CompanyReview, 0.10)]
    public void DefaultPercentage_matches_PB014_defaults(PaymentType type, double expected)
    {
        ProfitShareCalculator.DefaultPercentage(type).Should().Be((decimal)expected);
    }
}
