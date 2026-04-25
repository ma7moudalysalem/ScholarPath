using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ScholarPath.Application.Ai.Commands.LogRecommendationClick;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.UnitTests.Ai;

/// <summary>
/// Idempotency + correlation behavior for the recommendation-click command
/// (PB-017 T-002 / FR-249). Uses InMemory EF so we exercise the real query
/// path for the 500ms debounce window.
/// </summary>
public class LogRecommendationClickTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    private static (LogRecommendationClickCommandHandler handler, ApplicationDbContext db, Guid userId)
        BuildSut(Guid? scholarshipId = null, DateTimeOffset? now = null)
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new ApplicationDbContext(opts);

        var userId = Guid.NewGuid();
        var sId = scholarshipId ?? Guid.NewGuid();

        db.Scholarships.Add(new Scholarship
        {
            Id = sId,
            TitleEn = "Test",
            TitleAr = "اختبار",
            Slug = "test-" + sId.ToString("N")[..8],
            OwnerCompanyId = Guid.NewGuid(),
            Status = ScholarshipStatus.Open,
            FundingType = FundingType.FullyFunded,
            TargetLevel = AcademicLevel.Masters,
            Mode = ListingMode.InApp,
            DescriptionEn = "",
            DescriptionAr = "",
            Deadline = FixedNow.AddMonths(3),
            CreatedAt = FixedNow,
        });
        db.SaveChanges();

        var clock = Substitute.For<IDateTimeService>();
        clock.UtcNow.Returns(now ?? FixedNow);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);

        var handler = new LogRecommendationClickCommandHandler(
            (IApplicationDbContext)db, currentUser, clock, NullLogger<LogRecommendationClickCommandHandler>.Instance);

        return (handler, db, userId);
    }

    [Fact]
    public async Task First_click_persists_and_is_not_deduplicated()
    {
        var (handler, db, userId) = BuildSut();
        var scholarshipId = db.Scholarships.Single().Id;

        var result = await handler.Handle(
            new LogRecommendationClickCommand(scholarshipId, null, "card"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Deduplicated.Should().BeFalse();
        (await db.RecommendationClickEvents.CountAsync()).Should().Be(1);

        var row = await db.RecommendationClickEvents.SingleAsync();
        row.UserId.Should().Be(userId);
        row.ScholarshipId.Should().Be(scholarshipId);
        row.Source.Should().Be("card");
    }

    [Fact]
    public async Task Repeat_click_inside_500ms_is_deduplicated()
    {
        var (handler, db, _) = BuildSut();
        var scholarshipId = db.Scholarships.Single().Id;

        var first = await handler.Handle(
            new LogRecommendationClickCommand(scholarshipId, null, "card"),
            CancellationToken.None);
        var second = await handler.Handle(
            new LogRecommendationClickCommand(scholarshipId, null, "card"),
            CancellationToken.None);

        second!.Deduplicated.Should().BeTrue();
        second.EventId.Should().Be(first!.EventId);
        (await db.RecommendationClickEvents.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Click_after_debounce_window_persists_new_event()
    {
        // Arrange: seed first click, then advance the clock by > 500ms.
        var (handler, db, userId) = BuildSut();
        var scholarshipId = db.Scholarships.Single().Id;

        await handler.Handle(
            new LogRecommendationClickCommand(scholarshipId, null, "card"),
            CancellationToken.None);

        // Manually fabricate the "older" click and replace the handler with one
        // whose clock is 1 second ahead.
        var clock = Substitute.For<IDateTimeService>();
        clock.UtcNow.Returns(FixedNow.AddSeconds(1));
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        var later = new LogRecommendationClickCommandHandler(
            (IApplicationDbContext)db, currentUser, clock, NullLogger<LogRecommendationClickCommandHandler>.Instance);

        var second = await later.Handle(
            new LogRecommendationClickCommand(scholarshipId, null, "card"),
            CancellationToken.None);

        second!.Deduplicated.Should().BeFalse();
        (await db.RecommendationClickEvents.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Missing_scholarship_throws_NotFound()
    {
        var (handler, _, _) = BuildSut();

        var act = () => handler.Handle(
            new LogRecommendationClickCommand(Guid.NewGuid(), null, "card"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Stale_AiInteractionId_from_other_user_is_silently_dropped()
    {
        var (handler, db, _) = BuildSut();
        var scholarshipId = db.Scholarships.Single().Id;

        // Seed an interaction that belongs to a DIFFERENT user
        var otherId = Guid.NewGuid();
        var otherInteractionId = Guid.NewGuid();
        db.AiInteractions.Add(new AiInteraction
        {
            Id = otherInteractionId,
            UserId = otherId,
            Feature = AiFeature.Recommendation,
            Provider = AiProvider.Stub,
            PromptText = "x",
            ResponseText = "y",
            StartedAt = FixedNow.AddMinutes(-10),
            CreatedAt = FixedNow.AddMinutes(-10),
        });
        await db.SaveChangesAsync();

        var result = await handler.Handle(
            new LogRecommendationClickCommand(scholarshipId, otherInteractionId, "card"),
            CancellationToken.None);

        result!.Deduplicated.Should().BeFalse();
        var saved = await db.RecommendationClickEvents.SingleAsync();
        saved.AiInteractionId.Should().BeNull(); // correlation dropped, click still recorded
    }
}

public class LogRecommendationClickValidatorTests
{
    private readonly LogRecommendationClickCommandValidator _v = new();

    [Fact]
    public void Empty_scholarshipId_fails()
    {
        var r = _v.Validate(new LogRecommendationClickCommand(Guid.Empty, null, "card"));
        r.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("card")]
    [InlineData("list")]
    [InlineData("modal")]
    public void Allowed_source_passes(string src)
    {
        var r = _v.Validate(new LogRecommendationClickCommand(Guid.NewGuid(), null, src));
        r.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Unknown_source_fails()
    {
        var r = _v.Validate(new LogRecommendationClickCommand(Guid.NewGuid(), null, "tooltip"));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Null_source_passes()
    {
        var r = _v.Validate(new LogRecommendationClickCommand(Guid.NewGuid(), null, null));
        r.IsValid.Should().BeTrue();
    }
}
