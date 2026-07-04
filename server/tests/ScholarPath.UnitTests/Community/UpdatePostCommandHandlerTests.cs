using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Community.Commands.UpdatePost;

namespace ScholarPath.UnitTests.Community;

public sealed class UpdatePostCommandHandlerTests : IDisposable
{
    private readonly CommunityTestHarness _h = new();
    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task Author_can_update_own_root_post()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.AsStudent(_h.StudentA);
        var handler = new UpdatePostCommandHandler(_h.Db, _h.CurrentUser);

        var ok = await handler.Handle(
            new UpdatePostCommand(post.Id, "Updated title", "عنوان محدث", "Updated body", "جسم محدث"),
            CancellationToken.None);

        ok.Should().BeTrue();
        var saved = _h.Db.ForumPosts.Single(p => p.Id == post.Id);
        saved.TitleEn.Should().Be("Updated title");
        saved.TitleAr.Should().Be("عنوان محدث");
        saved.BodyEn.Should().Be("Updated body");
        saved.BodyAr.Should().Be("جسم محدث");
        saved.Title.Should().Be("Updated title");
        saved.BodyMarkdown.Should().Be("Updated body");
    }

    [Fact]
    public async Task Non_author_student_cannot_update_someone_elses_post()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.AsStudent(_h.StudentB);
        var handler = new UpdatePostCommandHandler(_h.Db, _h.CurrentUser);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(
                new UpdatePostCommand(post.Id, "hack", "هاك", "hack", "هاك"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Consultant_cannot_update_post_even_if_they_somehow_authored_one()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.AsConsultant();
        var handler = new UpdatePostCommandHandler(_h.Db, _h.CurrentUser);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(
                new UpdatePostCommand(post.Id, "x", "س", "y", "ص"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Reply_update_does_not_require_title()
    {
        var root = await _h.SeedPostAsync(_h.StudentA);
        var reply = await _h.SeedReplyAsync(_h.StudentA, root);
        _h.AsStudent(_h.StudentA);
        var handler = new UpdatePostCommandHandler(_h.Db, _h.CurrentUser);

        var ok = await handler.Handle(
            new UpdatePostCommand(reply.Id, null, null, "Edited reply text", null),
            CancellationToken.None);

        ok.Should().BeTrue();
        var saved = _h.Db.ForumPosts.Single(p => p.Id == reply.Id);
        saved.BodyMarkdown.Should().Be("Edited reply text");
        saved.BodyEn.Should().Be("Edited reply text");
    }

    [Fact]
    public async Task Root_update_replaces_tag_set()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.AsStudent(_h.StudentA);

        // Seed initial tags
        var tagA = new Domain.Entities.ForumTag { Name = "alpha", Slug = "alpha" };
        var tagB = new Domain.Entities.ForumTag { Name = "beta", Slug = "beta" };
        _h.Db.ForumTags.AddRange(tagA, tagB);
        _h.Db.ForumPostTags.AddRange(
            new Domain.Entities.ForumPostTag { ForumPostId = post.Id, ForumTagId = tagA.Id },
            new Domain.Entities.ForumPostTag { ForumPostId = post.Id, ForumTagId = tagB.Id });
        await _h.Db.SaveChangesAsync();

        var handler = new UpdatePostCommandHandler(_h.Db, _h.CurrentUser);
        await handler.Handle(
            new UpdatePostCommand(post.Id, "title", "عنوان", "body", "جسم", new[] { "gamma" }),
            CancellationToken.None);

        var slugs = _h.Db.ForumPostTags
            .Where(pt => pt.ForumPostId == post.Id)
            .Select(pt => pt.ForumTag!.Slug)
            .ToList();
        slugs.Should().BeEquivalentTo(new[] { "gamma" });
    }
}
