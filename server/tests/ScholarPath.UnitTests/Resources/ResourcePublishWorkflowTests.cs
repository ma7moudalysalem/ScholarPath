using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Resources.Commands.ApproveResource;
using ScholarPath.Application.Resources.Commands.RejectResource;
using ScholarPath.Application.Resources.Commands.SubmitResourceForReview;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Resources;

public class ResourcePublishWorkflowTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ICurrentUserService User(Guid id, params string[] roles)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.UserId.Returns(id);
        foreach (var r in roles) u.IsInRole(r).Returns(true);
        return u;
    }

    private static Resource Draft(Guid authorId, ResourceStatus status = ResourceStatus.Draft) => new()
    {
        Id = Guid.NewGuid(),
        TitleEn = "Title EN",
        TitleAr = "العنوان",
        Slug = $"slug-{Guid.NewGuid():N}",
        ContentMarkdownEn = "content en",
        ContentMarkdownAr = "محتوى عربي",
        CategorySlug = "essays",
        AuthorUserId = authorId,
        AuthorRole = "Consultant",
        Type = ResourceType.Article,
        Status = status,
    };

    private static void SeedBio(ApplicationDbContext db, Guid userId) =>
        db.UserProfiles.Add(new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Biography = "An experienced author bio.",
        });

    private static SubmitResourceForReviewCommandHandler SubmitSut(
        ApplicationDbContext db, ICurrentUserService user) =>
        new(db, user, NullLogger<SubmitResourceForReviewCommandHandler>.Instance);

    private static ApproveResourceCommandHandler ApproveSut(
        ApplicationDbContext db, ICurrentUserService user) =>
        new(db, user, Substitute.For<INotificationDispatcher>(),
            NullLogger<ApproveResourceCommandHandler>.Instance);

    private static RejectResourceCommandHandler RejectSut(
        ApplicationDbContext db, ICurrentUserService user) =>
        new(db, user, Substitute.For<INotificationDispatcher>(),
            NullLogger<RejectResourceCommandHandler>.Instance);

    [Fact]
    public async Task Submit_moves_complete_draft_to_pending_review()
    {
        using var db = CreateDb();
        var authorId = Guid.NewGuid();
        var resource = Draft(authorId);
        db.Resources.Add(resource);
        SeedBio(db, authorId);
        await db.SaveChangesAsync();

        var status = await SubmitSut(db, User(authorId, "Consultant"))
            .Handle(new SubmitResourceForReviewCommand(resource.Id), default);

        status.Should().Be(ResourceStatus.PendingReview);
    }

    [Fact]
    public async Task Submit_blocks_incomplete_draft()
    {
        using var db = CreateDb();
        var authorId = Guid.NewGuid();
        var resource = Draft(authorId);
        resource.ContentMarkdownEn = null;   // missing English content
        db.Resources.Add(resource);
        SeedBio(db, authorId);
        await db.SaveChangesAsync();

        var act = () => SubmitSut(db, User(authorId, "Consultant"))
            .Handle(new SubmitResourceForReviewCommand(resource.Id), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Submit_blocks_when_author_has_no_bio()
    {
        using var db = CreateDb();
        var authorId = Guid.NewGuid();
        db.Resources.Add(Draft(authorId));
        await db.SaveChangesAsync();   // no UserProfile / bio seeded

        var resourceId = db.Resources.Single().Id;
        var act = () => SubmitSut(db, User(authorId, "Consultant"))
            .Handle(new SubmitResourceForReviewCommand(resourceId), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Submit_by_admin_publishes_directly()
    {
        using var db = CreateDb();
        var adminId = Guid.NewGuid();
        var resource = Draft(adminId);
        db.Resources.Add(resource);
        SeedBio(db, adminId);
        await db.SaveChangesAsync();

        var status = await SubmitSut(db, User(adminId, "Admin"))
            .Handle(new SubmitResourceForReviewCommand(resource.Id), default);

        status.Should().Be(ResourceStatus.Published);
        (await db.Resources.FindAsync(resource.Id))!.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Approve_publishes_a_pending_resource()
    {
        using var db = CreateDb();
        var authorId = Guid.NewGuid();
        var resource = Draft(authorId, ResourceStatus.PendingReview);
        db.Resources.Add(resource);
        SeedBio(db, authorId);
        await db.SaveChangesAsync();

        await ApproveSut(db, User(Guid.NewGuid(), "Admin"))
            .Handle(new ApproveResourceCommand(resource.Id), default);

        (await db.Resources.FindAsync(resource.Id))!.Status.Should().Be(ResourceStatus.Published);
    }

    [Fact]
    public async Task Approve_rejects_a_resource_not_pending_review()
    {
        using var db = CreateDb();
        var resource = Draft(Guid.NewGuid());
        db.Resources.Add(resource);
        await db.SaveChangesAsync();

        var act = () => ApproveSut(db, User(Guid.NewGuid(), "Admin"))
            .Handle(new ApproveResourceCommand(resource.Id), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Reject_returns_resource_to_draft_with_reason()
    {
        using var db = CreateDb();
        var resource = Draft(Guid.NewGuid(), ResourceStatus.PendingReview);
        db.Resources.Add(resource);
        await db.SaveChangesAsync();

        await RejectSut(db, User(Guid.NewGuid(), "Admin"))
            .Handle(new RejectResourceCommand(resource.Id, "Needs more detail."), default);

        var updated = await db.Resources.FindAsync(resource.Id);
        updated!.Status.Should().Be(ResourceStatus.Draft);
        updated.RejectionReason.Should().Be("Needs more detail.");
    }
}
