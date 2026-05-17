using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Jobs;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Scholarships;

public sealed class ScholarshipAutoCloseJobTests : IDisposable
{
    private readonly ApplicationDbContext _db;

    public ScholarshipAutoCloseJobTests()
        => _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    private ScholarshipAutoCloseJob Sut()
        => new(_db, NullLogger<ScholarshipAutoCloseJob>.Instance);

    private static Scholarship Listing(ScholarshipStatus status, DateTimeOffset deadline) => new()
    {
        Id = Guid.NewGuid(),
        TitleEn = "Listing",
        TitleAr = "قائمة",
        DescriptionEn = "Description",
        DescriptionAr = "وصف",
        Slug = $"listing-{Guid.NewGuid():N}",
        Status = status,
        Deadline = deadline,
    };

    [Fact]
    public async Task RunAsync_ClosesOpenListingPastDeadline()
    {
        var expired = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddDays(-1));
        _db.Scholarships.Add(expired);
        await _db.SaveChangesAsync();

        await Sut().RunAsync(default);

        (await _db.Scholarships.FindAsync(expired.Id))!.Status
            .Should().Be(ScholarshipStatus.Closed);
    }

    [Fact]
    public async Task RunAsync_KeepsOpenListingBeforeDeadline()
    {
        var active = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddDays(7));
        _db.Scholarships.Add(active);
        await _db.SaveChangesAsync();

        await Sut().RunAsync(default);

        (await _db.Scholarships.FindAsync(active.Id))!.Status
            .Should().Be(ScholarshipStatus.Open);
    }

    [Fact]
    public async Task RunAsync_IgnoresNonOpenListingsPastDeadline()
    {
        var draft = Listing(ScholarshipStatus.Draft, DateTimeOffset.UtcNow.AddDays(-3));
        var archived = Listing(ScholarshipStatus.Archived, DateTimeOffset.UtcNow.AddDays(-3));
        var alreadyClosed = Listing(ScholarshipStatus.Closed, DateTimeOffset.UtcNow.AddDays(-3));
        _db.Scholarships.AddRange(draft, archived, alreadyClosed);
        await _db.SaveChangesAsync();

        await Sut().RunAsync(default);

        (await _db.Scholarships.FindAsync(draft.Id))!.Status
            .Should().Be(ScholarshipStatus.Draft);
        (await _db.Scholarships.FindAsync(archived.Id))!.Status
            .Should().Be(ScholarshipStatus.Archived);
        (await _db.Scholarships.FindAsync(alreadyClosed.Id))!.Status
            .Should().Be(ScholarshipStatus.Closed);
    }

    [Fact]
    public async Task RunAsync_ClosesOnlyTheExpiredOpenListings()
    {
        var expired1 = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddDays(-1));
        var expired2 = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddHours(-2));
        var future = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddDays(10));
        _db.Scholarships.AddRange(expired1, expired2, future);
        await _db.SaveChangesAsync();

        await Sut().RunAsync(default);

        (await _db.Scholarships.FindAsync(expired1.Id))!.Status.Should().Be(ScholarshipStatus.Closed);
        (await _db.Scholarships.FindAsync(expired2.Id))!.Status.Should().Be(ScholarshipStatus.Closed);
        (await _db.Scholarships.FindAsync(future.Id))!.Status.Should().Be(ScholarshipStatus.Open);
    }

    [Fact]
    public async Task RunAsync_NoListings_DoesNotThrow()
    {
        var act = () => Sut().RunAsync(default);

        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _db.Dispose();
}
