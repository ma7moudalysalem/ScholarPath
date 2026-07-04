using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.ConsultantBookings.Queries.GetMyReceivedReviews;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

public sealed class GetMyReceivedConsultantReviewsQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly Guid _consultantId = Guid.NewGuid();

    public GetMyReceivedConsultantReviewsQueryHandlerTests()
        => _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    private ICurrentUserService User(Guid? id = null)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.UserId.Returns(id ?? _consultantId);
        return u;
    }

    private GetMyReceivedConsultantReviewsQueryHandler Sut(ICurrentUserService user)
        => new(_db, user);

    private async Task<Guid> SeedStudentAsync(string first, string last)
    {
        var id = Guid.NewGuid();
        _db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = first,
            LastName = last,
            Email = $"{id:N}@scholarpath.local",
            UserName = $"{id:N}@scholarpath.local",
            AccountStatus = AccountStatus.Active,
        });
        await _db.SaveChangesAsync();
        return id;
    }

    private async Task SeedReviewAsync(
        Guid studentId,
        int rating,
        string? comment = null,
        DateTimeOffset? createdAt = null,
        Guid? consultantId = null,
        bool hidden = false,
        bool deleted = false)
    {
        _db.ConsultantReviews.Add(new ConsultantReview
        {
            Id = Guid.NewGuid(),
            BookingId = Guid.NewGuid(),
            StudentId = studentId,
            ConsultantId = consultantId ?? _consultantId,
            Rating = rating,
            Comment = comment,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            IsHiddenByAdmin = hidden,
            IsDeleted = deleted,
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// PB-006R: the summary average now comes from the persisted rating snapshot
    /// (ConsultantAverageRating), which ConsultantRatingService keeps current in
    /// production. Mirror that invariant here (factor 1.0, penalized == raw).
    /// </summary>
    private async Task SyncSnapshotAsync(Guid? consultantId = null)
    {
        var cid = consultantId ?? _consultantId;
        var visible = await _db.ConsultantReviews
            .Where(r => r.ConsultantId == cid && !r.IsHiddenByAdmin && !r.IsDeleted)
            .ToListAsync();
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == cid);
        if (profile is null)
        {
            profile = new UserProfile { UserId = cid };
            _db.UserProfiles.Add(profile);
        }
        profile.ConsultantReviewCount = visible.Count;
        profile.ConsultantAverageRating = visible.Count == 0
            ? null
            : Math.Round((decimal)visible.Average(r => r.Rating), 2, MidpointRounding.AwayFromZero);
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_NotAuthenticated_ThrowsForbidden()
    {
        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns((Guid?)null);

        var act = () => Sut(user).Handle(new GetMyReceivedConsultantReviewsQuery(), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_NoReviews_ReturnsEmptySummary()
    {
        var result = await Sut(User()).Handle(new GetMyReceivedConsultantReviewsQuery(), default);

        result.TotalReviews.Should().Be(0);
        result.AverageRating.Should().Be(0);
        result.Reviews.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AggregatesAverageAndCount_OverVisibleReviewsOnly()
    {
        var s1 = await SeedStudentAsync("Sarah", "Adams");
        var s2 = await SeedStudentAsync("Omar", "Khan");
        await SeedReviewAsync(s1, 5);
        await SeedReviewAsync(s2, 2);
        await SeedReviewAsync(s1, 1, hidden: true);
        await SeedReviewAsync(s2, 1, deleted: true);
        await SeedReviewAsync(s1, 1, consultantId: Guid.NewGuid());
        await SyncSnapshotAsync();

        var result = await Sut(User()).Handle(new GetMyReceivedConsultantReviewsQuery(), default);

        result.TotalReviews.Should().Be(2);
        result.AverageRating.Should().Be(3.5);
        result.Reviews.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_MasksAuthorName_FirstNamePlusInitials()
    {
        var s = await SeedStudentAsync("Mohamed", "Aly Salem");
        await SeedReviewAsync(s, 5, comment: "Insightful session.");

        var result = await Sut(User()).Handle(new GetMyReceivedConsultantReviewsQuery(), default);

        result.Reviews.Should().ContainSingle()
            .Which.AuthorName.Should().Be("Mohamed A. S.");
    }

    [Fact]
    public async Task Handle_OrdersNewestFirst()
    {
        var s = await SeedStudentAsync("Sarah", "Adams");
        await SeedReviewAsync(s, 3, comment: "older", createdAt: DateTimeOffset.UtcNow.AddDays(-3));
        await SeedReviewAsync(s, 5, comment: "newer", createdAt: DateTimeOffset.UtcNow);

        var result = await Sut(User()).Handle(new GetMyReceivedConsultantReviewsQuery(), default);

        result.Reviews.Select(r => r.Comment).Should().ContainInOrder("newer", "older");
    }

    public void Dispose() => _db.Dispose();
}
