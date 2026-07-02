using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Resources.Commands.CompleteResourceChapter;
using ScholarPath.Application.Resources.Commands.ToggleResourceBookmark;
using ScholarPath.Application.Resources.Queries.GetResourceProgressDetail;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Resources;

public class ResourceStudentActionsTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ICurrentUserService User(Guid id)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.UserId.Returns(id);
        return u;
    }

    private static Resource PublishedResource() => new()
    {
        Id = Guid.NewGuid(),
        TitleEn = "T", TitleAr = "ت",
        Slug = $"slug-{Guid.NewGuid():N}",
        AuthorUserId = Guid.NewGuid(),
        AuthorRole = "Consultant",
        Type = ResourceType.Guide,
        Status = ResourceStatus.Published,
    };

    [Fact]
    public async Task Bookmark_toggle_adds_then_removes()
    {
        using var db = CreateDb();
        var resource = PublishedResource();
        db.Resources.Add(resource);
        await db.SaveChangesAsync();
        var studentId = Guid.NewGuid();
        var sut = new ToggleResourceBookmarkCommandHandler(db, User(studentId));

        var first = await sut.Handle(new ToggleResourceBookmarkCommand(resource.Id), default);
        first.Should().BeTrue();
        (await db.ResourceBookmarks.CountAsync()).Should().Be(1);

        var second = await sut.Handle(new ToggleResourceBookmarkCommand(resource.Id), default);
        second.Should().BeFalse();
        (await db.ResourceBookmarks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Bookmark_unknown_resource_throws()
    {
        using var db = CreateDb();
        var sut = new ToggleResourceBookmarkCommandHandler(db, User(Guid.NewGuid()));

        var act = () => sut.Handle(new ToggleResourceBookmarkCommand(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Complete_chapter_records_progress()
    {
        using var db = CreateDb();
        var resource = PublishedResource();
        var chapter = new ResourceChild
        {
            Id = Guid.NewGuid(),
            ResourceId = resource.Id,
            TitleEn = "Ch", TitleAr = "فصل",
            SortOrder = 0,
        };
        db.Resources.Add(resource);
        db.ResourceChapters.Add(chapter);
        await db.SaveChangesAsync();

        var sut = new CompleteResourceChapterCommandHandler(db, User(Guid.NewGuid()));
        var result = await sut.Handle(
            new CompleteResourceChapterCommand(resource.Id, chapter.Id), default);

        result.ChaptersCompletedCount.Should().Be(1);
        result.TotalChapters.Should().Be(1);
        (await db.ResourceProgress.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Complete_unknown_chapter_throws()
    {
        using var db = CreateDb();
        var resource = PublishedResource();
        db.Resources.Add(resource);
        await db.SaveChangesAsync();

        var sut = new CompleteResourceChapterCommandHandler(db, User(Guid.NewGuid()));
        var act = () => sut.Handle(
            new CompleteResourceChapterCommand(resource.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Complete_chapter_twice_is_idempotent()
    {
        using var db = CreateDb();
        var resource = PublishedResource();
        var chapter = new ResourceChild
        {
            Id = Guid.NewGuid(),
            ResourceId = resource.Id,
            TitleEn = "Ch", TitleAr = "فصل",
            SortOrder = 0,
        };
        db.Resources.Add(resource);
        db.ResourceChapters.Add(chapter);
        await db.SaveChangesAsync();

        var studentId = Guid.NewGuid();
        var sut = new CompleteResourceChapterCommandHandler(db, User(studentId));
        var cmd = new CompleteResourceChapterCommand(resource.Id, chapter.Id);

        await sut.Handle(cmd, default);
        var second = await sut.Handle(cmd, default);

        second.ChaptersCompletedCount.Should().Be(1);
        (await db.ResourceProgress.CountAsync()).Should().Be(1);
        (await db.ResourceProgressChildren.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Progress_detail_returns_completed_chapter_ids()
    {
        using var db = CreateDb();
        var resource = PublishedResource();
        var chapterOne = new ResourceChild
        {
            Id = Guid.NewGuid(), ResourceId = resource.Id,
            TitleEn = "One", TitleAr = "١", SortOrder = 0,
        };
        var chapterTwo = new ResourceChild
        {
            Id = Guid.NewGuid(), ResourceId = resource.Id,
            TitleEn = "Two", TitleAr = "٢", SortOrder = 1,
        };
        db.Resources.Add(resource);
        db.ResourceChapters.AddRange(chapterOne, chapterTwo);
        await db.SaveChangesAsync();

        var studentId = Guid.NewGuid();
        await new CompleteResourceChapterCommandHandler(db, User(studentId))
            .Handle(new CompleteResourceChapterCommand(resource.Id, chapterOne.Id), default);

        var progress = await new GetResourceProgressDetailQueryHandler(db, User(studentId))
            .Handle(new GetResourceProgressDetailQuery(resource.Id), default);

        progress.TotalChapters.Should().Be(2);
        progress.ChaptersCompletedCount.Should().Be(1);
        progress.CompletedChapterIds.Should().ContainSingle().Which.Should().Be(chapterOne.Id);
    }

    [Fact]
    public async Task Progress_detail_is_empty_when_nothing_completed()
    {
        using var db = CreateDb();
        var resource = PublishedResource();
        var chapter = new ResourceChild
        {
            Id = Guid.NewGuid(), ResourceId = resource.Id,
            TitleEn = "Ch", TitleAr = "فصل", SortOrder = 0,
        };
        db.Resources.Add(resource);
        db.ResourceChapters.Add(chapter);
        await db.SaveChangesAsync();

        var progress = await new GetResourceProgressDetailQueryHandler(db, User(Guid.NewGuid()))
            .Handle(new GetResourceProgressDetailQuery(resource.Id), default);

        progress.TotalChapters.Should().Be(1);
        progress.ChaptersCompletedCount.Should().Be(0);
        progress.CompletedChapterIds.Should().BeEmpty();
    }
}
