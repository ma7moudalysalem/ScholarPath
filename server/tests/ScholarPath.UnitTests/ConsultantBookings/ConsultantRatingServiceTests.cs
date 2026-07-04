using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.Services;
using ScholarPath.Domain.Entities;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>
/// Unit-tests the PB-006R consultant rating snapshot + penalty-factor formula:
/// penalized average = round(clamp(rawAvg * factor, 0, 5), 2), factor persists
/// across recomputes, sticky low-rating flag on the penalized average.
/// </summary>
public sealed class ConsultantRatingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationDispatcher _notifications = Substitute.For<INotificationDispatcher>();
    private readonly ConsultantRatingService _sut;
    private readonly Guid _consultantId = Guid.NewGuid();

    public ConsultantRatingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _sut = new ConsultantRatingService(_db, _notifications, NullLogger<ConsultantRatingService>.Instance);
    }

    private async Task SeedProfileAsync(decimal factor = 1.0m)
    {
        _db.UserProfiles.Add(new UserProfile
        {
            UserId = _consultantId,
            ConsultantRatingPenaltyFactor = factor,
        });
        await _db.SaveChangesAsync();
    }

    private async Task AddReviewsAsync(params int[] ratings)
    {
        foreach (var r in ratings)
        {
            _db.ConsultantReviews.Add(new ConsultantReview
            {
                BookingId = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                ConsultantId = _consultantId,
                Rating = r,
            });
        }
        await _db.SaveChangesAsync();
    }

    private async Task<UserProfile> ReloadProfileAsync() =>
        await _db.UserProfiles.AsNoTracking().FirstAsync(p => p.UserId == _consultantId);

    [Fact]
    public async Task Factor_one_gives_the_raw_average()
    {
        await SeedProfileAsync(factor: 1.0m);
        await AddReviewsAsync(4, 5, 3); // avg 4.00

        await _sut.RecalculateSnapshotAsync(_consultantId, default);

        var profile = await ReloadProfileAsync();
        profile.ConsultantAverageRating.Should().Be(4.00m);
        profile.ConsultantReviewCount.Should().Be(3);
    }

    [Fact]
    public async Task Penalty_factor_reduces_the_displayed_average()
    {
        await SeedProfileAsync(factor: 0.60m); // a validated no-show already applied −40%
        await AddReviewsAsync(4, 5, 3); // raw avg 4.00 → 4.00 * 0.60 = 2.40

        await _sut.RecalculateSnapshotAsync(_consultantId, default);

        var profile = await ReloadProfileAsync();
        profile.ConsultantAverageRating.Should().Be(2.40m);
    }

    [Fact]
    public async Task Zero_reviews_yields_null_average_even_with_penalty()
    {
        await SeedProfileAsync(factor: 0.30m);

        await _sut.RecalculateSnapshotAsync(_consultantId, default);

        var profile = await ReloadProfileAsync();
        profile.ConsultantAverageRating.Should().BeNull();
        profile.ConsultantReviewCount.Should().Be(0);
        // Factor is preserved so it applies once the first review lands.
        profile.ConsultantRatingPenaltyFactor.Should().Be(0.30m);
    }

    [Fact]
    public async Task Hidden_and_deleted_reviews_are_excluded()
    {
        await SeedProfileAsync();
        await AddReviewsAsync(5, 5); // visible
        _db.ConsultantReviews.Add(new ConsultantReview
        {
            BookingId = Guid.NewGuid(), StudentId = Guid.NewGuid(), ConsultantId = _consultantId,
            Rating = 1, IsHiddenByAdmin = true,
        });
        _db.ConsultantReviews.Add(new ConsultantReview
        {
            BookingId = Guid.NewGuid(), StudentId = Guid.NewGuid(), ConsultantId = _consultantId,
            Rating = 1, IsDeleted = true,
        });
        await _db.SaveChangesAsync();

        await _sut.RecalculateSnapshotAsync(_consultantId, default);

        var profile = await ReloadProfileAsync();
        profile.ConsultantAverageRating.Should().Be(5.00m);
        profile.ConsultantReviewCount.Should().Be(2);
    }

    [Fact]
    public async Task Penalized_average_below_threshold_sets_sticky_flag()
    {
        await SeedProfileAsync(factor: 0.60m);
        await AddReviewsAsync(4, 4, 4); // raw 4.00 → 2.40 penalized, < 2.5

        await _sut.RecalculateSnapshotAsync(_consultantId, default);
        var firstFlag = (await ReloadProfileAsync()).ConsultantLowRatingFlaggedAt;
        firstFlag.Should().NotBeNull();

        // A later recompute must NOT overwrite the original flag timestamp.
        await AddReviewsAsync(1);
        await _sut.RecalculateSnapshotAsync(_consultantId, default);
        (await ReloadProfileAsync()).ConsultantLowRatingFlaggedAt.Should().Be(firstFlag);
    }

    [Fact]
    public async Task ApplyPenaltyFactor_compounds_and_recomputes()
    {
        await SeedProfileAsync(factor: 1.0m);
        await AddReviewsAsync(5, 5); // raw 5.00

        await _sut.ApplyPenaltyFactorAsync(_consultantId, 0.80m, default); // consultant cancel <24h

        var profile = await ReloadProfileAsync();
        profile.ConsultantRatingPenaltyFactor.Should().Be(0.80m);
        profile.ConsultantAverageRating.Should().Be(4.00m); // 5.00 * 0.80
    }

    public void Dispose() => _db.Dispose();
}
