using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.FinancialConfig;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.FinancialConfig;

public sealed class FinancialRuleResolverTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static FinancialConfigRule Rule(
        PaymentType type, FeeKind feeKind, decimal? feePct, long? feeAmount,
        decimal profitShare, FinancialRuleStatus status) => new()
    {
        Id = Guid.NewGuid(),
        PaymentType = type,
        FeeKind = feeKind,
        FeePercentage = feePct,
        FeeAmountCents = feeAmount,
        ProfitSharePercentage = profitShare,
        Status = status,
        EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-1),
        SetByAdminId = Guid.NewGuid(),
    };

    [Fact]
    public async Task Falls_back_to_the_default_profit_share_when_nothing_is_configured()
    {
        using var db = CreateDb();

        var split = await FinancialRuleResolver.ResolvePaymentSplitAsync(
            db, PaymentType.ConsultantBooking, 10_000, default);

        // Default ConsultantBooking profit-share is 10%.
        split.PlatformTakeCents.Should().Be(1_000);
        split.PayeeNetCents.Should().Be(9_000);
    }

    [Fact]
    public async Task Falls_back_to_the_legacy_profit_share_config_when_no_rule_is_active()
    {
        using var db = CreateDb();
        db.ProfitShareConfigs.Add(new ProfitShareConfig
        {
            Id = Guid.NewGuid(),
            PaymentType = PaymentType.ConsultantBooking,
            Percentage = 0.20m,
            EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-2),
            EffectiveTo = null,
            SetByAdminId = Guid.NewGuid(),
        });
        await db.SaveChangesAsync();

        var split = await FinancialRuleResolver.ResolvePaymentSplitAsync(
            db, PaymentType.ConsultantBooking, 10_000, default);

        split.PlatformTakeCents.Should().Be(2_000);
        split.PayeeNetCents.Should().Be(8_000);
    }

    [Fact]
    public async Task Uses_the_active_percentage_fee_rule()
    {
        using var db = CreateDb();
        db.FinancialConfigRules.Add(Rule(
            PaymentType.ConsultantBooking, FeeKind.Percentage, 0.05m, null,
            0.10m, FinancialRuleStatus.Active));
        await db.SaveChangesAsync();

        var split = await FinancialRuleResolver.ResolvePaymentSplitAsync(
            db, PaymentType.ConsultantBooking, 10_000, default);

        // 5% fee (500) + 10% profit-share (1000) = 1500 platform, 8500 payee.
        split.PlatformTakeCents.Should().Be(1_500);
        split.PayeeNetCents.Should().Be(8_500);
    }

    [Fact]
    public async Task Uses_the_active_fixed_fee_rule()
    {
        using var db = CreateDb();
        db.FinancialConfigRules.Add(Rule(
            PaymentType.CompanyReview, FeeKind.FixedAmount, null, 300,
            0.10m, FinancialRuleStatus.Active));
        await db.SaveChangesAsync();

        var split = await FinancialRuleResolver.ResolvePaymentSplitAsync(
            db, PaymentType.CompanyReview, 10_000, default);

        // $3.00 fee (300) + 10% profit-share (1000) = 1300 platform, 8700 payee.
        split.PlatformTakeCents.Should().Be(1_300);
        split.PayeeNetCents.Should().Be(8_700);
    }

    [Theory]
    [InlineData(FinancialRuleStatus.Draft)]
    [InlineData(FinancialRuleStatus.Archived)]
    public async Task Ignores_a_rule_that_is_not_active(FinancialRuleStatus status)
    {
        using var db = CreateDb();
        db.FinancialConfigRules.Add(Rule(
            PaymentType.ConsultantBooking, FeeKind.Percentage, 0.30m, null,
            0.30m, status));
        await db.SaveChangesAsync();

        var split = await FinancialRuleResolver.ResolvePaymentSplitAsync(
            db, PaymentType.ConsultantBooking, 10_000, default);

        // The non-active rule is ignored — legacy default profit-share (10%) applies.
        split.PlatformTakeCents.Should().Be(1_000);
    }

    [Fact]
    public async Task Caps_the_platform_take_at_the_gross_for_an_oversized_fixed_fee()
    {
        using var db = CreateDb();
        // A $50 fixed fee against a $3 gross — the platform take must not exceed gross.
        db.FinancialConfigRules.Add(Rule(
            PaymentType.ConsultantBooking, FeeKind.FixedAmount, null, 5_000,
            0.10m, FinancialRuleStatus.Active));
        await db.SaveChangesAsync();

        var split = await FinancialRuleResolver.ResolvePaymentSplitAsync(
            db, PaymentType.ConsultantBooking, 300, default);

        split.PlatformTakeCents.Should().Be(300);
        split.PayeeNetCents.Should().Be(0);
    }

    [Fact]
    public async Task Resolves_each_payment_type_independently()
    {
        using var db = CreateDb();
        db.FinancialConfigRules.Add(Rule(
            PaymentType.CompanyReview, FeeKind.Percentage, 0.05m, null,
            0.10m, FinancialRuleStatus.Active));
        await db.SaveChangesAsync();

        var review = await FinancialRuleResolver.ResolvePaymentSplitAsync(
            db, PaymentType.CompanyReview, 10_000, default);
        var booking = await FinancialRuleResolver.ResolvePaymentSplitAsync(
            db, PaymentType.ConsultantBooking, 10_000, default);

        review.PlatformTakeCents.Should().Be(1_500);
        // ConsultantBooking has no rule — legacy default profit-share (10%).
        booking.PlatformTakeCents.Should().Be(1_000);
    }

    [Fact]
    public async Task Platform_take_and_payee_net_always_resum_to_gross()
    {
        using var db = CreateDb();
        db.FinancialConfigRules.Add(Rule(
            PaymentType.ConsultantBooking, FeeKind.Percentage, 0.07m, null,
            0.13m, FinancialRuleStatus.Active));
        await db.SaveChangesAsync();

        var split = await FinancialRuleResolver.ResolvePaymentSplitAsync(
            db, PaymentType.ConsultantBooking, 99_999, default);

        (split.PlatformTakeCents + split.PayeeNetCents).Should().Be(99_999);
    }
}
