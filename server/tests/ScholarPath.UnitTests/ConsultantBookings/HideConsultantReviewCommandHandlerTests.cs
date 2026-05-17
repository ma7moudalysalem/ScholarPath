using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.ConsultantBookings.Commands.HideConsultantReview;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

public sealed class HideConsultantReviewCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;

    public HideConsultantReviewCommandHandlerTests()
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

    private HideConsultantReviewCommandHandler Sut(ICurrentUserService user)
        => new(_db, user, NullLogger<HideConsultantReviewCommandHandler>.Instance);

    private async Task<ConsultantReview> SeedReviewAsync(bool hidden = false)
    {
        var review = new ConsultantReview
        {
            Id = Guid.NewGuid(),
            BookingId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            ConsultantId = Guid.NewGuid(),
            Rating = 1,
            Comment = "Unhelpful session.",
            IsHiddenByAdmin = hidden,
        };
        _db.ConsultantReviews.Add(review);
        await _db.SaveChangesAsync();
        return review;
    }

    [Fact]
    public async Task Handle_AdminHides_SetsHiddenFlagAndNote()
    {
        var review = await SeedReviewAsync();

        await Sut(User("Admin")).Handle(
            new HideConsultantReviewCommand(review.Id, true, "Off-topic rant."), default);

        var updated = await _db.ConsultantReviews.FindAsync(review.Id);
        updated!.IsHiddenByAdmin.Should().BeTrue();
        updated.AdminNote.Should().Be("Off-topic rant.");
    }

    [Fact]
    public async Task Handle_AdminUnhides_ClearsHiddenFlag()
    {
        var review = await SeedReviewAsync(hidden: true);

        await Sut(User("Admin")).Handle(
            new HideConsultantReviewCommand(review.Id, false, null), default);

        var updated = await _db.ConsultantReviews.FindAsync(review.Id);
        updated!.IsHiddenByAdmin.Should().BeFalse();
        updated.AdminNote.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NonAdmin_ThrowsForbidden()
    {
        var review = await SeedReviewAsync();

        var act = () => Sut(User("Consultant")).Handle(
            new HideConsultantReviewCommand(review.Id, true, null), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_UnknownReview_ThrowsNotFound()
    {
        var act = () => Sut(User("Admin")).Handle(
            new HideConsultantReviewCommand(Guid.NewGuid(), true, null), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedReview_ThrowsNotFound()
    {
        var review = await SeedReviewAsync();
        review.IsDeleted = true;
        await _db.SaveChangesAsync();

        var act = () => Sut(User("Admin")).Handle(
            new HideConsultantReviewCommand(review.Id, true, null), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose() => _db.Dispose();
}
