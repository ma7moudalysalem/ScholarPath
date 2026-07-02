using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Resources;
using ScholarPath.Application.Resources.Commands.CreateResource;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Resources;

public class CreateResourceCommandHandlerTests
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

    private static CreateResourceCommandHandler Sut(ApplicationDbContext db, ICurrentUserService user) =>
        new(db, user, NullLogger<CreateResourceCommandHandler>.Instance);

    private static CreateResourceCommand Cmd(IReadOnlyList<ResourceChapterInput>? chapters = null) =>
        new("English Title", "عنوان عربي", "desc en", "desc ar",
            "content en", "content ar", null, null,
            ResourceType.Article, "essays", new[] { "tag1" }, chapters);

    [Fact]
    public async Task Creates_draft_resource_for_consultant()
    {
        using var db = CreateDb();
        var id = Guid.NewGuid();

        var resourceId = await Sut(db, User(id, "Consultant")).Handle(Cmd(), default);

        var resource = await db.Resources.FindAsync(resourceId);
        resource.Should().NotBeNull();
        resource!.Status.Should().Be(ResourceStatus.Draft);
        resource.AuthorRole.Should().Be("Consultant");
        resource.AuthorUserId.Should().Be(id);
        resource.Slug.Should().StartWith("english-title-");
    }

    [Fact]
    public async Task Rejects_non_author_role()
    {
        using var db = CreateDb();

        var act = () => Sut(db, User(Guid.NewGuid(), "Student")).Handle(Cmd(), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Persists_chapters_in_sort_order()
    {
        using var db = CreateDb();
        var chapters = new[]
        {
            new ResourceChapterInput("Ch1 en", "Ch1 ar", "c1 en", "c1 ar", 0, 5),
            new ResourceChapterInput("Ch2 en", "Ch2 ar", "c2 en", "c2 ar", 1, 8),
        };

        var resourceId = await Sut(db, User(Guid.NewGuid(), "ScholarshipProvider")).Handle(Cmd(chapters), default);

        var saved = await db.ResourceChapters
            .Where(c => c.ResourceId == resourceId)
            .CountAsync();
        saved.Should().Be(2);
    }

    [Fact]
    public void Validator_requires_both_titles()
    {
        var v = new CreateResourceCommandValidator();

        v.Validate(Cmd() with { TitleEn = "" }).IsValid.Should().BeFalse();
        v.Validate(Cmd() with { TitleAr = "" }).IsValid.Should().BeFalse();
        v.Validate(Cmd()).IsValid.Should().BeTrue();
    }
}
