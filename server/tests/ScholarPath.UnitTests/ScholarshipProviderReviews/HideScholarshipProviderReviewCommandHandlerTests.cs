using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.ScholarshipProviderReviews.Commands.HideScholarshipProviderReview;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ScholarshipProviderReviews;

public sealed class HideScholarshipProviderReviewCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;

    public HideScholarshipProviderReviewCommandHandlerTests()
        => _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    private static ICurrentUserService User(params string[] roles)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.UserId.Returns(Guid.NewGuid());
        foreach (var r in roles) u.IsInRole(r).Returns(true);
        return u;
    }

    private HideScholarshipProviderReviewCommandHandler Sut(ICurrentUserService user)
        => new(_db, user, NullLogger<HideScholarshipProviderReviewCommandHandler>.Instance);

    private async Task<ScholarshipProviderReview> SeedReviewAsync(bool hidden = false)
    {
        var review = new ScholarshipProviderReview
        {
            Id = Guid.NewGuid(),
            ApplicationTrackerId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            ScholarshipProviderId = Guid.NewGuid(),
            Rating = 2,
            Comment = "Disappointing.",
            IsHiddenByAdmin = hidden,
        };
        _db.ScholarshipProviderReviews.Add(review);
        await _db.SaveChangesAsync();
        return review;
    }

    [Fact]
    public async Task Handle_AdminHides_SetsHiddenFlagAndNote()
    {
        var review = await SeedReviewAsync();

        await Sut(User("Admin")).Handle(
            new HideScholarshipProviderReviewCommand(review.Id, true, "Abusive language."), default);

        var updated = await _db.ScholarshipProviderReviews.FindAsync(review.Id);
        updated!.IsHiddenByAdmin.Should().BeTrue();
        updated.AdminNote.Should().Be("Abusive language.");
    }

    [Fact]
    public async Task Handle_AdminUnhides_ClearsHiddenFlag()
    {
        var review = await SeedReviewAsync(hidden: true);

        await Sut(User("Admin")).Handle(
            new HideScholarshipProviderReviewCommand(review.Id, false, null), default);

        var updated = await _db.ScholarshipProviderReviews.FindAsync(review.Id);
        updated!.IsHiddenByAdmin.Should().BeFalse();
        updated.AdminNote.Should().BeNull();
    }

    [Fact]
    public async Task Handle_BlankNote_StoredAsNull()
    {
        var review = await SeedReviewAsync();

        await Sut(User("Admin")).Handle(
            new HideScholarshipProviderReviewCommand(review.Id, true, "   "), default);

        (await _db.ScholarshipProviderReviews.FindAsync(review.Id))!.AdminNote.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NonAdmin_ThrowsForbidden()
    {
        var review = await SeedReviewAsync();

        var act = () => Sut(User("Student")).Handle(
            new HideScholarshipProviderReviewCommand(review.Id, true, null), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_UnknownReview_ThrowsNotFound()
    {
        var act = () => Sut(User("Admin")).Handle(
            new HideScholarshipProviderReviewCommand(Guid.NewGuid(), true, null), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedReview_ThrowsNotFound()
    {
        var review = await SeedReviewAsync();
        review.IsDeleted = true;
        await _db.SaveChangesAsync();

        var act = () => Sut(User("Admin")).Handle(
            new HideScholarshipProviderReviewCommand(review.Id, true, null), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose() => _db.Dispose();
}
