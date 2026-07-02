using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.ScholarshipProviderReviews.Queries.GetMyReceivedReviews;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ScholarshipProviderReviews;

public sealed class GetMyReceivedReviewsQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly Guid _companyId = Guid.NewGuid();

    public GetMyReceivedReviewsQueryHandlerTests()
        => _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    private ICurrentUserService User(Guid? id = null)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.UserId.Returns(id ?? _companyId);
        return u;
    }

    private GetMyReceivedReviewsQueryHandler Sut(ICurrentUserService user) => new(_db, user);

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
        Guid? companyId = null,
        bool hidden = false,
        bool deleted = false)
    {
        _db.ScholarshipProviderReviews.Add(new ScholarshipProviderReview
        {
            Id = Guid.NewGuid(),
            ApplicationTrackerId = Guid.NewGuid(),
            StudentId = studentId,
            ScholarshipProviderId = companyId ?? _companyId,
            Rating = rating,
            Comment = comment,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            IsHiddenByAdmin = hidden,
            IsDeleted = deleted,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_NotAuthenticated_ThrowsForbidden()
    {
        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns((Guid?)null);

        var act = () => Sut(user).Handle(new GetMyReceivedReviewsQuery(), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_NoReviews_ReturnsEmptySummary()
    {
        var result = await Sut(User()).Handle(new GetMyReceivedReviewsQuery(), default);

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
        await SeedReviewAsync(s2, 4);
        // Hidden, deleted, and another company's review must be excluded.
        await SeedReviewAsync(s1, 1, hidden: true);
        await SeedReviewAsync(s2, 1, deleted: true);
        await SeedReviewAsync(s1, 1, companyId: Guid.NewGuid());

        var result = await Sut(User()).Handle(new GetMyReceivedReviewsQuery(), default);

        result.TotalReviews.Should().Be(2);
        result.AverageRating.Should().Be(4.5);
        result.Reviews.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_MasksAuthorName_FirstNamePlusInitials()
    {
        var s = await SeedStudentAsync("Sarah", "Adams");
        await SeedReviewAsync(s, 5, comment: "Great support.");

        var result = await Sut(User()).Handle(new GetMyReceivedReviewsQuery(), default);

        result.Reviews.Should().ContainSingle()
            .Which.AuthorName.Should().Be("Sarah A.");
    }

    [Fact]
    public async Task Handle_OrdersNewestFirst()
    {
        var s = await SeedStudentAsync("Sarah", "Adams");
        await SeedReviewAsync(s, 3, comment: "older", createdAt: DateTimeOffset.UtcNow.AddDays(-5));
        await SeedReviewAsync(s, 5, comment: "newer", createdAt: DateTimeOffset.UtcNow);

        var result = await Sut(User()).Handle(new GetMyReceivedReviewsQuery(), default);

        result.Reviews.Select(r => r.Comment).Should().ContainInOrder("newer", "older");
    }

    public void Dispose() => _db.Dispose();
}
