using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Scholarships.Queries;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Scholarships;

/// <summary>
/// Covers <see cref="GetMyBookmarkedScholarshipsQuery"/> and
/// <see cref="GetFeaturedScholarshipsQuery"/> — the bookmarks-list and
/// featured-scholarships read models. Uses the EF Core in-memory provider
/// (same as <see cref="ScholarshipAutoCloseJobTests"/>) — the SQLite provider
/// cannot translate <c>ORDER BY</c> over <c>DateTimeOffset</c>, which both
/// queries rely on (SavedAt / Deadline).
/// </summary>
public sealed class ScholarshipBookmarksAndFeaturedTests : IDisposable
{
    private readonly ApplicationDbContext _db;

    public ScholarshipBookmarksAndFeaturedTests()
        => _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Scholarship NewScholarship(
        ScholarshipStatus status = ScholarshipStatus.Open,
        bool isFeatured = false,
        int featuredOrder = 0,
        DateTimeOffset? deadline = null,
        bool isDeleted = false,
        string titleEn = "Test Scholarship",
        string titleAr = "منحة تجريبية") => new()
    {
        Id = Guid.NewGuid(),
        TitleEn = titleEn,
        TitleAr = titleAr,
        DescriptionEn = "Desc EN",
        DescriptionAr = "وصف",
        Slug = $"sch-{Guid.NewGuid():N}",
        Status = status,
        IsFeatured = isFeatured,
        FeaturedOrder = featuredOrder,
        Deadline = deadline ?? DateTimeOffset.UtcNow.AddDays(30),
        FundingType = FundingType.FullyFunded,
        TargetLevel = AcademicLevel.Masters,
        IsDeleted = isDeleted,
    };

    private ICurrentUserService UserStub(Guid? userId)
    {
        var stub = Substitute.For<ICurrentUserService>();
        stub.UserId.Returns(userId);
        return stub;
    }

    // ── GetMyBookmarkedScholarshipsQuery ───────────────────────────────────────

    [Fact]
    public async Task Bookmarks_ReturnsOnlyTheCurrentUsersSavedScholarships()
    {
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();

        var mine = NewScholarship();
        var theirs = NewScholarship();
        _db.Scholarships.AddRange(mine, theirs);
        _db.SavedScholarships.AddRange(
            new SavedScholarship { UserId = me, ScholarshipId = mine.Id, SavedAt = DateTimeOffset.UtcNow },
            new SavedScholarship { UserId = other, ScholarshipId = theirs.Id, SavedAt = DateTimeOffset.UtcNow });
        await _db.SaveChangesAsync();

        var handler = new GetMyBookmarkedScholarshipsQueryHandler(_db, UserStub(me));

        var result = await handler.Handle(new GetMyBookmarkedScholarshipsQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].ScholarshipId.Should().Be(mine.Id);
        result[0].Scholarship.Title.Should().Be("Test Scholarship");
    }

    [Fact]
    public async Task Bookmarks_AreOrderedNewestSavedFirst()
    {
        var me = Guid.NewGuid();
        var older = NewScholarship(titleEn: "Older");
        var newer = NewScholarship(titleEn: "Newer");
        _db.Scholarships.AddRange(older, newer);
        _db.SavedScholarships.AddRange(
            new SavedScholarship { UserId = me, ScholarshipId = older.Id, SavedAt = DateTimeOffset.UtcNow.AddDays(-5) },
            new SavedScholarship { UserId = me, ScholarshipId = newer.Id, SavedAt = DateTimeOffset.UtcNow.AddHours(-1) });
        await _db.SaveChangesAsync();

        var handler = new GetMyBookmarkedScholarshipsQueryHandler(_db, UserStub(me));

        var result = await handler.Handle(new GetMyBookmarkedScholarshipsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Scholarship.Title.Should().Be("Newer");
        result[1].Scholarship.Title.Should().Be("Older");
    }

    [Fact]
    public async Task Bookmarks_DropOrphanedRows_WhenScholarshipIsSoftDeleted()
    {
        var me = Guid.NewGuid();
        var live = NewScholarship();
        var deleted = NewScholarship(isDeleted: true);
        _db.Scholarships.AddRange(live, deleted);
        _db.SavedScholarships.AddRange(
            new SavedScholarship { UserId = me, ScholarshipId = live.Id, SavedAt = DateTimeOffset.UtcNow },
            new SavedScholarship { UserId = me, ScholarshipId = deleted.Id, SavedAt = DateTimeOffset.UtcNow });
        await _db.SaveChangesAsync();

        var handler = new GetMyBookmarkedScholarshipsQueryHandler(_db, UserStub(me));

        var result = await handler.Handle(new GetMyBookmarkedScholarshipsQuery(), CancellationToken.None);

        // The bookmark whose scholarship was soft-deleted simply drops out.
        result.Should().ContainSingle();
        result[0].ScholarshipId.Should().Be(live.Id);
    }

    [Fact]
    public async Task Bookmarks_LocalisesTitleToArabic_WhenLanguageIsAr()
    {
        var me = Guid.NewGuid();
        var sch = NewScholarship();
        _db.Scholarships.Add(sch);
        _db.SavedScholarships.Add(
            new SavedScholarship { UserId = me, ScholarshipId = sch.Id, SavedAt = DateTimeOffset.UtcNow });
        await _db.SaveChangesAsync();

        var handler = new GetMyBookmarkedScholarshipsQueryHandler(_db, UserStub(me));

        var result = await handler.Handle(
            new GetMyBookmarkedScholarshipsQuery { Language = "ar" }, CancellationToken.None);

        result[0].Scholarship.Title.Should().Be("منحة تجريبية");
    }

    [Fact]
    public async Task Bookmarks_ReturnsEmptyList_WhenUserHasNoBookmarks()
    {
        var handler = new GetMyBookmarkedScholarshipsQueryHandler(_db, UserStub(Guid.NewGuid()));

        var result = await handler.Handle(new GetMyBookmarkedScholarshipsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Bookmarks_ThrowsForbidden_WhenNotAuthenticated()
    {
        var handler = new GetMyBookmarkedScholarshipsQueryHandler(_db, UserStub(null));

        var act = () => handler.Handle(new GetMyBookmarkedScholarshipsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    // ── GetFeaturedScholarshipsQuery ───────────────────────────────────────────

    [Fact]
    public async Task Featured_ReturnsOnlyFeaturedOpenNonDeletedScholarships()
    {
        var featuredOpen   = NewScholarship(isFeatured: true);
        var notFeatured    = NewScholarship(isFeatured: false);
        var featuredDraft  = NewScholarship(status: ScholarshipStatus.Draft, isFeatured: true);
        var featuredClosed = NewScholarship(status: ScholarshipStatus.Closed, isFeatured: true);
        var featuredDeleted = NewScholarship(isFeatured: true, isDeleted: true);
        _db.Scholarships.AddRange(featuredOpen, notFeatured, featuredDraft, featuredClosed, featuredDeleted);
        await _db.SaveChangesAsync();

        var handler = new GetFeaturedScholarshipsQueryHandler(_db);

        var result = await handler.Handle(new GetFeaturedScholarshipsQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(featuredOpen.Id);
    }

    [Fact]
    public async Task Featured_AreOrderedByFeaturedOrderThenDeadline()
    {
        var third  = NewScholarship(isFeatured: true, featuredOrder: 3, titleEn: "Third");
        var first  = NewScholarship(isFeatured: true, featuredOrder: 1, titleEn: "First");
        var second = NewScholarship(isFeatured: true, featuredOrder: 2, titleEn: "Second");
        _db.Scholarships.AddRange(third, first, second);
        await _db.SaveChangesAsync();

        var handler = new GetFeaturedScholarshipsQueryHandler(_db);

        var result = await handler.Handle(new GetFeaturedScholarshipsQuery(), CancellationToken.None);

        result.Select(r => r.Title).Should().ContainInOrder("First", "Second", "Third");
    }

    [Fact]
    public async Task Featured_RespectsTheLimit()
    {
        for (var i = 0; i < 5; i++)
            _db.Scholarships.Add(NewScholarship(isFeatured: true, featuredOrder: i));
        await _db.SaveChangesAsync();

        var handler = new GetFeaturedScholarshipsQueryHandler(_db);

        var result = await handler.Handle(
            new GetFeaturedScholarshipsQuery { Limit = 3 }, CancellationToken.None);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Featured_LocalisesTitleToArabic_WhenLanguageIsAr()
    {
        _db.Scholarships.Add(NewScholarship(isFeatured: true));
        await _db.SaveChangesAsync();

        var handler = new GetFeaturedScholarshipsQueryHandler(_db);

        var result = await handler.Handle(
            new GetFeaturedScholarshipsQuery { Language = "ar" }, CancellationToken.None);

        result[0].Title.Should().Be("منحة تجريبية");
    }

    [Fact]
    public async Task Featured_ReturnsEmptyList_WhenNoneAreFeatured()
    {
        _db.Scholarships.Add(NewScholarship(isFeatured: false));
        await _db.SaveChangesAsync();

        var handler = new GetFeaturedScholarshipsQueryHandler(_db);

        var result = await handler.Handle(new GetFeaturedScholarshipsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    public void Dispose() => _db.Dispose();
}
