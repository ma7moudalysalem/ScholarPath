using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Community.Commands.CreatePost;

namespace ScholarPath.UnitTests.Community;

public sealed class CreatePostCommandHandlerTests : IDisposable
{
    private readonly CommunityTestHarness _h = new();

    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task Student_creates_post_successfully()
    {
        _h.AsStudent(_h.StudentA);
        var handler = new CreatePostCommandHandler(_h.Db, _h.CurrentUser);

        var id = await handler.Handle(
            new CreatePostCommand(_h.Category.Id, "First post", "Body content"),
            CancellationToken.None);

        var saved = _h.Db.ForumPosts.Single(p => p.Id == id);
        saved.AuthorId.Should().Be(_h.StudentA.Id);
        saved.Title.Should().Be("First post");
        saved.BodyMarkdown.Should().Be("Body content");
        saved.ParentPostId.Should().BeNull();
    }

    [Fact]
    public async Task Consultant_cannot_create_post()
    {
        _h.AsConsultant();
        var handler = new CreatePostCommandHandler(_h.Db, _h.CurrentUser);

        var act = () => handler.Handle(
            new CreatePostCommand(_h.Category.Id, "x", "y"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Company_cannot_create_post()
    {
        _h.AsCompany();
        var handler = new CreatePostCommandHandler(_h.Db, _h.CurrentUser);

        var act = () => handler.Handle(
            new CreatePostCommand(_h.Category.Id, "x", "y"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Tags_are_normalized_and_attached()
    {
        _h.AsStudent(_h.StudentA);
        var handler = new CreatePostCommandHandler(_h.Db, _h.CurrentUser);

        var id = await handler.Handle(
            new CreatePostCommand(
                _h.Category.Id,
                "With tags",
                "Body",
                new[] { "Visa", "  visa  ", "study-abroad", "Study Abroad" }),
            CancellationToken.None);

        var saved = _h.Db.ForumPosts.Single(p => p.Id == id);
        var slugs = _h.Db.ForumPostTags
            .Where(pt => pt.ForumPostId == saved.Id)
            .Select(pt => pt.ForumTag!.Slug)
            .OrderBy(s => s)
            .ToList();

        // "Visa"/"  visa  " collapse to "visa"; "Study Abroad" and "study-abroad" both -> "study-abroad".
        slugs.Should().BeEquivalentTo(new[] { "study-abroad", "visa" });
    }

    [Fact]
    public async Task More_than_five_tags_throws_validation_exception()
    {
        _h.AsStudent(_h.StudentA);
        var handler = new CreatePostCommandHandler(_h.Db, _h.CurrentUser);

        var act = () => handler.Handle(
            new CreatePostCommand(
                _h.Category.Id,
                "Title",
                "Body",
                new[] { "a", "b", "c", "d", "e", "f" }),
            CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
