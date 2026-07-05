using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>
/// Covers <see cref="ConsultantReadService"/> — the consultant-marketplace read
/// projections (browse / detail / open-slots). Uses the EF Core in-memory
/// provider so the Identity join-tables (<c>UserRoles</c> / <c>Roles</c>) and
/// the booking + availability entities can be seeded together.
/// </summary>
public sealed class ConsultantReadServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IDateTimeService _clock = Substitute.For<IDateTimeService>();
    private readonly IHttpContextAccessor _http = Substitute.For<IHttpContextAccessor>();
    private readonly ConsultantReadService _service;

    // A fixed "now" so recurring-slot expansion is deterministic.
    // 2026-05-18 is a Monday.
    private static readonly DateTimeOffset Now =
        new(2026, 5, 18, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _consultantRoleId = Guid.NewGuid();

    public ConsultantReadServiceTests()
    {
        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        _clock.UtcNow.Returns(Now);
        _service = new ConsultantReadService(_db, _clock, _http);

        _db.Roles.Add(new ApplicationRole
        {
            Id = _consultantRoleId,
            Name = "Consultant",
            NormalizedName = "CONSULTANT",
        });
        _db.SaveChanges();
    }

    // ── Seed helpers ────────────────────────────────────────────────────────────

    private Guid SeedConsultant(
        string first = "Sarah",
        string last = "Adel",
        AccountStatus status = AccountStatus.Active,
        bool inRole = true,
        decimal? fee = 35m,
        int? durationMinutes = 45,
        string? expertiseJson = null,
        string? languagesJson = null,
        string? bio = "Helps students with scholarships.",
        bool verified = true)
    {
        var id = Guid.NewGuid();
        var email = $"c-{id:N}@test.com";
        _db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = first,
            LastName = last,
            Email = email,
            UserName = email,
            AccountStatus = status,
            Profile = new UserProfile
            {
                UserId = id,
                Biography = bio,
                SessionFeeUsd = fee,
                SessionDurationMinutes = durationMinutes,
                ExpertiseTagsJson = expertiseJson,
                LanguagesJson = languagesJson,
                // Marketplace visibility requires the official verification
                // marker (PB-006 / consultant-eligibility). Seed it by default so
                // the pre-existing projection tests still surface the consultant.
                ConsultantVerifiedAt = verified ? Now.AddDays(-30) : null,
            },
        });

        if (inRole)
        {
            _db.UserRoles.Add(new IdentityUserRole<Guid>
            {
                UserId = id,
                RoleId = _consultantRoleId,
            });
        }

        _db.SaveChanges();
        return id;
    }

    private Guid SeedStudent(string first = "Tasneem", string last = "Shaban")
    {
        var id = Guid.NewGuid();
        var email = $"s-{id:N}@test.com";
        _db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = first,
            LastName = last,
            Email = email,
            UserName = email,
            AccountStatus = AccountStatus.Active,
        });
        _db.SaveChanges();
        return id;
    }

    private void SeedApprovedConsultantUpgrade(Guid userId)
    {
        _db.UpgradeRequests.Add(new UpgradeRequest
        {
            UserId = userId,
            Target = UpgradeTarget.Consultant,
            Status = UpgradeRequestStatus.Approved,
            CreatedAt = Now.AddDays(-5),
        });
        _db.SaveChanges();
    }

    private void SeedReview(Guid consultantId, Guid studentId, int rating, bool hidden = false)
    {
        _db.ConsultantReviews.Add(new ConsultantReview
        {
            Id = Guid.NewGuid(),
            BookingId = Guid.NewGuid(),
            ConsultantId = consultantId,
            StudentId = studentId,
            Rating = rating,
            Comment = $"Rated {rating}",
            IsHiddenByAdmin = hidden,
            CreatedAt = Now.AddDays(-1),
        });
        _db.SaveChanges();
        SyncRatingSnapshot(consultantId);
    }

    /// <summary>
    /// PB-006R: the read service now reads the persisted rating snapshot, which
    /// ConsultantRatingService keeps current in production. Mirror that invariant in
    /// tests (penalty factor stays 1.0, so penalized average == raw average).
    /// </summary>
    private void SyncRatingSnapshot(Guid consultantId)
    {
        var visible = _db.ConsultantReviews
            .Where(r => r.ConsultantId == consultantId && !r.IsHiddenByAdmin && !r.IsDeleted)
            .ToList();
        var profile = _db.UserProfiles.First(p => p.UserId == consultantId);
        profile.ConsultantReviewCount = visible.Count;
        profile.ConsultantAverageRating = visible.Count == 0
            ? null
            : Math.Round((decimal)visible.Average(r => r.Rating), 2, MidpointRounding.AwayFromZero);
        _db.SaveChanges();
    }

    private void SeedBooking(
        Guid consultantId, Guid studentId, BookingStatus status,
        DateTimeOffset start, int durationMinutes = 45)
    {
        _db.Bookings.Add(new ConsultantBooking
        {
            Id = Guid.NewGuid(),
            ConsultantId = consultantId,
            StudentId = studentId,
            ScheduledStartAt = start,
            ScheduledEndAt = start.AddMinutes(durationMinutes),
            DurationMinutes = durationMinutes,
            PriceUsd = 35m,
            Status = status,
        });
        _db.SaveChanges();
    }

    private void SeedRecurringAvailability(
        Guid consultantId, DayOfWeek day, TimeOnly start, TimeOnly end, bool active = true)
    {
        _db.Availabilities.Add(new ConsultantAvailability
        {
            Id = Guid.NewGuid(),
            ConsultantId = consultantId,
            IsRecurring = true,
            DayOfWeek = day,
            StartTime = start,
            EndTime = end,
            Timezone = "UTC",
            IsActive = active,
        });
        _db.SaveChanges();
    }

    private void SeedAdHocAvailability(
        Guid consultantId, DateTimeOffset start, DateTimeOffset end)
    {
        _db.Availabilities.Add(new ConsultantAvailability
        {
            Id = Guid.NewGuid(),
            ConsultantId = consultantId,
            IsRecurring = false,
            SpecificStartAt = start,
            SpecificEndAt = end,
            Timezone = "UTC",
            IsActive = true,
        });
        _db.SaveChanges();
    }

    // ── BrowseConsultantsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task Browse_ReturnsOnlyActiveUsersInTheConsultantRole()
    {
        var consultant = SeedConsultant(first: "Sarah");
        SeedConsultant(first: "Suspended", status: AccountStatus.Suspended);
        SeedConsultant(first: "NotAConsultant", inRole: false);
        SeedStudent();

        var result = await _service.BrowseConsultantsAsync(CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(consultant);
        result[0].Name.Should().Be("Sarah Adel");
    }

    [Fact]
    public async Task Browse_ProjectsProfileFeeExpertiseAndLanguages()
    {
        SeedConsultant(
            fee: 40m,
            durationMinutes: 60,
            expertiseJson: """["Scholarship Strategy","Visa Support"]""",
            languagesJson: """["English","Arabic"]""");

        var result = await _service.BrowseConsultantsAsync(CancellationToken.None);

        var card = result.Should().ContainSingle().Subject;
        card.SessionFeeUsd.Should().Be(40m);
        card.SessionDurationMinutes.Should().Be(60);
        card.ExpertiseTags.Should().BeEquivalentTo("Scholarship Strategy", "Visa Support");
        card.Languages.Should().BeEquivalentTo("English", "Arabic");
    }

    [Fact]
    public async Task Browse_MalformedExpertiseJson_YieldsEmptyList()
    {
        SeedConsultant(expertiseJson: "not-json");

        var result = await _service.BrowseConsultantsAsync(CancellationToken.None);

        result[0].ExpertiseTags.Should().BeEmpty();
    }

    [Fact]
    public async Task Browse_AggregatesAverageRatingFromVisibleReviewsOnly()
    {
        var consultant = SeedConsultant();
        var student = SeedStudent();
        SeedReview(consultant, student, rating: 5);
        SeedReview(consultant, student, rating: 3);
        SeedReview(consultant, student, rating: 1, hidden: true); // excluded

        var result = await _service.BrowseConsultantsAsync(CancellationToken.None);

        result[0].ReviewCount.Should().Be(2);
        result[0].AverageRating.Should().Be(4d); // (5 + 3) / 2
    }

    [Fact]
    public async Task Browse_AverageRatingIsNull_WhenNoVisibleReviews()
    {
        SeedConsultant();

        var result = await _service.BrowseConsultantsAsync(CancellationToken.None);

        result[0].AverageRating.Should().BeNull();
        result[0].ReviewCount.Should().Be(0);
    }

    [Fact]
    public async Task Browse_CountsCompletedSessionsAndActiveAvailabilityRules()
    {
        var consultant = SeedConsultant();
        var student = SeedStudent();
        SeedBooking(consultant, student, BookingStatus.Completed, Now.AddDays(-10));
        SeedBooking(consultant, student, BookingStatus.Completed, Now.AddDays(-5));
        SeedBooking(consultant, student, BookingStatus.Confirmed, Now.AddDays(3)); // not completed
        SeedRecurringAvailability(consultant, DayOfWeek.Tuesday,
            new TimeOnly(16, 0), new TimeOnly(18, 0));
        SeedRecurringAvailability(consultant, DayOfWeek.Thursday,
            new TimeOnly(10, 0), new TimeOnly(12, 0), active: false); // inactive

        var result = await _service.BrowseConsultantsAsync(CancellationToken.None);

        result[0].CompletedSessionCount.Should().Be(2);
        result[0].ActiveAvailabilityRuleCount.Should().Be(1);
        result[0].HasAvailability.Should().BeTrue();
    }

    [Fact]
    public async Task Browse_ReturnsEmptyList_WhenNoConsultantsExist()
    {
        SeedStudent();

        var result = await _service.BrowseConsultantsAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Browse_ExcludesActiveConsultantWithoutVerificationMarker()
    {
        // An active user in the Consultant role but with no verification marker
        // and no approved upgrade must NOT appear in the public marketplace.
        SeedConsultant(first: "Unverified", verified: false);

        var result = await _service.BrowseConsultantsAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Browse_IncludesConsultantVerifiedByApprovedUpgrade()
    {
        // No ConsultantVerifiedAt marker, but an approved consultant upgrade
        // request is an equally valid approval signal (historical accounts).
        var consultant = SeedConsultant(first: "ByUpgrade", verified: false);
        SeedApprovedConsultantUpgrade(consultant);

        var result = await _service.BrowseConsultantsAsync(CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(consultant);
    }

    // ── GetConsultantDetailAsync ────────────────────────────────────────────────

    [Fact]
    public async Task Detail_ReturnsNull_WhenIdIsNotAConsultant()
    {
        var student = SeedStudent();

        var result = await _service.GetConsultantDetailAsync(student, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Detail_ReturnsNull_WhenConsultantIsNotActive()
    {
        var suspended = SeedConsultant(status: AccountStatus.Suspended);

        var result = await _service.GetConsultantDetailAsync(suspended, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Detail_ReturnsNull_WhenConsultantIsNotVerified()
    {
        var unverified = SeedConsultant(verified: false);

        var result = await _service.GetConsultantDetailAsync(unverified, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Detail_ReturnsProfileWithRecentVisibleReviewsNewestFirst()
    {
        var consultant = SeedConsultant(bio: "Detailed bio");
        var student = SeedStudent();

        _db.ConsultantReviews.Add(new ConsultantReview
        {
            Id = Guid.NewGuid(), BookingId = Guid.NewGuid(),
            ConsultantId = consultant, StudentId = student,
            Rating = 4, Comment = "Older", CreatedAt = Now.AddDays(-10),
        });
        _db.ConsultantReviews.Add(new ConsultantReview
        {
            Id = Guid.NewGuid(), BookingId = Guid.NewGuid(),
            ConsultantId = consultant, StudentId = student,
            Rating = 5, Comment = "Newer", CreatedAt = Now.AddDays(-1),
        });
        _db.ConsultantReviews.Add(new ConsultantReview
        {
            Id = Guid.NewGuid(), BookingId = Guid.NewGuid(),
            ConsultantId = consultant, StudentId = student,
            Rating = 1, Comment = "Hidden", IsHiddenByAdmin = true,
            CreatedAt = Now,
        });
        await _db.SaveChangesAsync();
        SyncRatingSnapshot(consultant);

        var result = await _service.GetConsultantDetailAsync(consultant, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Biography.Should().Be("Detailed bio");
        result.ReviewCount.Should().Be(2);
        result.AverageRating.Should().Be(4.5d);
        result.RecentReviews.Should().HaveCount(2);
        result.RecentReviews[0].Comment.Should().Be("Newer");
        result.RecentReviews[1].Comment.Should().Be("Older");
        result.RecentReviews[0].StudentName.Should().Be("Tasneem Shaban");
    }

    // ── GetConsultantOpenSlotsAsync ─────────────────────────────────────────────

    [Fact]
    public async Task OpenSlots_ReturnsNull_WhenIdIsNotAConsultant()
    {
        var student = SeedStudent();

        var result = await _service.GetConsultantOpenSlotsAsync(student, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task OpenSlots_ReturnsEmptyList_WhenConsultantHasNoAvailabilityRules()
    {
        var consultant = SeedConsultant();

        var result = await _service.GetConsultantOpenSlotsAsync(consultant, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task OpenSlots_ReturnsNull_WhenConsultantIsNotVerified()
    {
        // Even with saved availability, an unverified consultant exposes no
        // bookable slots to the public.
        var unverified = SeedConsultant(verified: false);
        SeedRecurringAvailability(unverified, DayOfWeek.Tuesday,
            new TimeOnly(16, 0), new TimeOnly(17, 0));

        var result = await _service.GetConsultantOpenSlotsAsync(unverified, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task OpenSlots_ExpandsRecurringRuleIntoFutureDatedSlots()
    {
        var consultant = SeedConsultant(durationMinutes: 45);
        // Now is Monday 2026-05-18 09:00Z. A Tuesday 16:00-17:00 rule sliced by
        // the 45-minute session yields one bookable slot per Tuesday (16:00).
        SeedRecurringAvailability(consultant, DayOfWeek.Tuesday,
            new TimeOnly(16, 0), new TimeOnly(17, 0));

        var result = await _service.GetConsultantOpenSlotsAsync(consultant, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();
        result.Should().OnlyContain(s => s.StartAt.DayOfWeek == DayOfWeek.Tuesday);
        result.Should().OnlyContain(s => s.StartAt > Now);
        result.Should().OnlyContain(s => s.DurationMinutes == 45);
        result[0].IsRecurring.Should().BeTrue();
        // First Tuesday after Mon 2026-05-18 is 2026-05-19 16:00Z.
        result[0].StartAt.Should().Be(new DateTimeOffset(2026, 5, 19, 16, 0, 0, TimeSpan.Zero));
        result[0].EndAt.Should().Be(new DateTimeOffset(2026, 5, 19, 16, 45, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task OpenSlots_SlicesAWideWindowIntoBackToBackSessionSlots()
    {
        // A 60-minute session over a Tuesday 16:00-19:00 (3-hour) window yields
        // exactly three back-to-back slots: 16:00, 17:00, 18:00 — not one
        // un-bookable 3-hour block.
        var consultant = SeedConsultant(durationMinutes: 60);
        SeedRecurringAvailability(consultant, DayOfWeek.Tuesday,
            new TimeOnly(16, 0), new TimeOnly(19, 0));

        var result = await _service.GetConsultantOpenSlotsAsync(consultant, CancellationToken.None);

        result.Should().NotBeNull();
        // First Tuesday after Mon 2026-05-18 is 2026-05-19.
        var firstTuesday = result!
            .Where(s => s.StartAt.UtcDateTime.Date == new DateTime(2026, 5, 19))
            .OrderBy(s => s.StartAt)
            .ToList();
        firstTuesday.Should().HaveCount(3);
        firstTuesday.Should().OnlyContain(s => s.DurationMinutes == 60);
        firstTuesday[0].StartAt.Should().Be(new DateTimeOffset(2026, 5, 19, 16, 0, 0, TimeSpan.Zero));
        firstTuesday[1].StartAt.Should().Be(new DateTimeOffset(2026, 5, 19, 17, 0, 0, TimeSpan.Zero));
        firstTuesday[2].StartAt.Should().Be(new DateTimeOffset(2026, 5, 19, 18, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task OpenSlots_YieldsNoSlots_WhenWindowIsShorterThanTheSession()
    {
        // Regression: a consultant with a 45-minute session who saved narrow
        // 15-minute windows (Monday 17:00-17:15) gets ZERO bookable slots — a
        // 45-minute session cannot be sliced out of a 15-minute window. This is
        // the silent mismatch the availability editor now warns about before
        // saving; the read side is asserted here so the behaviour is pinned.
        var consultant = SeedConsultant(durationMinutes: 45);
        SeedRecurringAvailability(consultant, DayOfWeek.Tuesday,
            new TimeOnly(17, 0), new TimeOnly(17, 15));

        var result = await _service.GetConsultantOpenSlotsAsync(consultant, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task OpenSlots_ReturnsWholeWindow_WhenConsultantHasNoSessionLength()
    {
        // With no configured session length the window stands whole (the booking
        // handler skips the duration check then), so even a narrow window is
        // bookable — contrast with the too-short-window case above.
        var consultant = SeedConsultant(durationMinutes: null);
        SeedRecurringAvailability(consultant, DayOfWeek.Tuesday,
            new TimeOnly(17, 0), new TimeOnly(17, 15));

        var result = await _service.GetConsultantOpenSlotsAsync(consultant, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Where(s => s.StartAt.UtcDateTime.Date == new DateTime(2026, 5, 19))
            .Should().ContainSingle()
            .Which.DurationMinutes.Should().Be(15);
    }

    [Fact]
    public async Task OpenSlots_IncludesFutureAdHocSlot_AndExcludesPastOne()
    {
        var consultant = SeedConsultant();
        SeedAdHocAvailability(consultant, Now.AddDays(2), Now.AddDays(2).AddMinutes(45));
        SeedAdHocAvailability(consultant, Now.AddDays(-2), Now.AddDays(-2).AddMinutes(45));

        var result = await _service.GetConsultantOpenSlotsAsync(consultant, CancellationToken.None);

        result.Should().ContainSingle();
        result![0].IsRecurring.Should().BeFalse();
        result[0].StartAt.Should().Be(Now.AddDays(2));
    }

    [Fact]
    public async Task OpenSlots_ExcludesAdHocSlotBeyondTheHorizon()
    {
        var consultant = SeedConsultant();
        SeedAdHocAvailability(consultant, Now.AddDays(60), Now.AddDays(60).AddMinutes(45));

        var result = await _service.GetConsultantOpenSlotsAsync(consultant, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task OpenSlots_RemovesWindowsAlreadyTakenByALiveBooking()
    {
        var consultant = SeedConsultant();
        var student = SeedStudent();

        // Tuesday 2026-05-19 16:00-17:00 ad-hoc slot.
        var slotStart = new DateTimeOffset(2026, 5, 19, 16, 0, 0, TimeSpan.Zero);
        SeedAdHocAvailability(consultant, slotStart, slotStart.AddMinutes(60));
        // A confirmed booking overlapping that exact window.
        SeedBooking(consultant, student, BookingStatus.Confirmed, slotStart, 60);

        var result = await _service.GetConsultantOpenSlotsAsync(consultant, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task OpenSlots_KeepsWindow_WhenOverlappingBookingIsCancelled()
    {
        var consultant = SeedConsultant();
        var student = SeedStudent();

        var slotStart = new DateTimeOffset(2026, 5, 19, 16, 0, 0, TimeSpan.Zero);
        SeedAdHocAvailability(consultant, slotStart, slotStart.AddMinutes(60));
        // A cancelled booking must NOT block the slot.
        SeedBooking(consultant, student, BookingStatus.Cancelled, slotStart, 60);

        var result = await _service.GetConsultantOpenSlotsAsync(consultant, CancellationToken.None);

        result.Should().ContainSingle();
        result![0].StartAt.Should().Be(slotStart);
    }

    [Fact]
    public async Task OpenSlots_IgnoresInactiveAndDeletedAvailabilityRules()
    {
        var consultant = SeedConsultant();
        SeedRecurringAvailability(consultant, DayOfWeek.Tuesday,
            new TimeOnly(16, 0), new TimeOnly(17, 0), active: false);

        var result = await _service.GetConsultantOpenSlotsAsync(consultant, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    public void Dispose() => _db.Dispose();
}
