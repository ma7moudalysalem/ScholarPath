using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.ProfitShare;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ProfitShare;

public sealed class ProfitShareConfigResolverTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ProfitShareConfig Config(
        PaymentType type, decimal pct,
        DateTimeOffset effectiveFrom, DateTimeOffset? effectiveTo) => new()
    {
        Id = Guid.NewGuid(),
        PaymentType = type,
        Percentage = pct,
        EffectiveFrom = effectiveFrom,
        EffectiveTo = effectiveTo,
        SetByAdminId = Guid.NewGuid(),
    };

    [Theory]
    [InlineData(PaymentType.ConsultantBooking)]
    [InlineData(PaymentType.ScholarshipProviderReview)]
    public async Task Falls_back_to_the_default_rate_when_no_config_exists(PaymentType type)
    {
        using var db = CreateDb();

        var pct = await ProfitShareConfigResolver.ResolveActivePercentageAsync(
            db, type, default);

        pct.Should().Be(ProfitShareCalculator.DefaultPercentage(type));
    }

    [Fact]
    public async Task Returns_the_percentage_of_an_open_ended_active_config()
    {
        using var db = CreateDb();
        db.ProfitShareConfigs.Add(Config(
            PaymentType.ConsultantBooking, 0.22m,
            DateTimeOffset.UtcNow.AddDays(-3), effectiveTo: null));
        await db.SaveChangesAsync();

        var pct = await ProfitShareConfigResolver.ResolveActivePercentageAsync(
            db, PaymentType.ConsultantBooking, default);

        pct.Should().Be(0.22m);
    }

    [Fact]
    public async Task Returns_a_config_whose_effective_window_is_still_open()
    {
        // A config scheduled to end in the future is still in force *now* — the
        // resolver must read it (the pre-unification capture query, filtering on
        // EffectiveTo == null, would have missed this and used the default).
        using var db = CreateDb();
        db.ProfitShareConfigs.Add(Config(
            PaymentType.ScholarshipProviderReview, 0.18m,
            DateTimeOffset.UtcNow.AddDays(-3),
            effectiveTo: DateTimeOffset.UtcNow.AddDays(3)));
        await db.SaveChangesAsync();

        var pct = await ProfitShareConfigResolver.ResolveActivePercentageAsync(
            db, PaymentType.ScholarshipProviderReview, default);

        pct.Should().Be(0.18m);
    }

    [Fact]
    public async Task Ignores_a_config_that_is_not_yet_effective()
    {
        // A future-dated config must not be applied — even when open-ended.
        using var db = CreateDb();
        db.ProfitShareConfigs.Add(Config(
            PaymentType.ConsultantBooking, 0.30m,
            DateTimeOffset.UtcNow.AddDays(5), effectiveTo: null));
        await db.SaveChangesAsync();

        var pct = await ProfitShareConfigResolver.ResolveActivePercentageAsync(
            db, PaymentType.ConsultantBooking, default);

        pct.Should().Be(ProfitShareCalculator.DefaultPercentage(PaymentType.ConsultantBooking));
    }

    [Fact]
    public async Task Ignores_a_config_whose_window_has_already_closed()
    {
        using var db = CreateDb();
        db.ProfitShareConfigs.Add(Config(
            PaymentType.ConsultantBooking, 0.30m,
            DateTimeOffset.UtcNow.AddDays(-10),
            effectiveTo: DateTimeOffset.UtcNow.AddDays(-2)));
        await db.SaveChangesAsync();

        var pct = await ProfitShareConfigResolver.ResolveActivePercentageAsync(
            db, PaymentType.ConsultantBooking, default);

        pct.Should().Be(ProfitShareCalculator.DefaultPercentage(PaymentType.ConsultantBooking));
    }

    [Fact]
    public async Task Picks_the_most_recently_effective_config_when_several_overlap()
    {
        using var db = CreateDb();
        db.ProfitShareConfigs.Add(Config(
            PaymentType.ConsultantBooking, 0.10m,
            DateTimeOffset.UtcNow.AddDays(-10), effectiveTo: null));
        db.ProfitShareConfigs.Add(Config(
            PaymentType.ConsultantBooking, 0.25m,
            DateTimeOffset.UtcNow.AddDays(-1), effectiveTo: null));
        await db.SaveChangesAsync();

        var pct = await ProfitShareConfigResolver.ResolveActivePercentageAsync(
            db, PaymentType.ConsultantBooking, default);

        pct.Should().Be(0.25m); // the later EffectiveFrom wins
    }

    [Fact]
    public async Task Resolves_each_payment_type_independently()
    {
        using var db = CreateDb();
        db.ProfitShareConfigs.Add(Config(
            PaymentType.ScholarshipProviderReview, 0.40m,
            DateTimeOffset.UtcNow.AddDays(-1), effectiveTo: null));
        await db.SaveChangesAsync();

        // ConsultantBooking has no config — it must not pick up the ScholarshipProviderReview row.
        var booking = await ProfitShareConfigResolver.ResolveActivePercentageAsync(
            db, PaymentType.ConsultantBooking, default);
        var review = await ProfitShareConfigResolver.ResolveActivePercentageAsync(
            db, PaymentType.ScholarshipProviderReview, default);

        booking.Should().Be(ProfitShareCalculator.DefaultPercentage(PaymentType.ConsultantBooking));
        review.Should().Be(0.40m);
    }
}
