using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Community.Commands.ToggleBookmark;

namespace ScholarPath.UnitTests.Community;

public sealed class ToggleBookmarkCommandHandlerTests : IDisposable
{
    private readonly CommunityTestHarness _h = new();
    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task Student_can_bookmark_then_unbookmark()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.AsStudent(_h.StudentB);
        var handler = new ToggleBookmarkCommandHandler(_h.Db, _h.CurrentUser);

        var firstResult = await handler.Handle(new ToggleBookmarkCommand(post.Id), CancellationToken.None);
        firstResult.Should().BeTrue();
        _h.Db.ForumBookmarks.Count(b => b.ForumPostId == post.Id && b.UserId == _h.StudentB.Id).Should().Be(1);

        var secondResult = await handler.Handle(new ToggleBookmarkCommand(post.Id), CancellationToken.None);
        secondResult.Should().BeFalse();
        _h.Db.ForumBookmarks.Count(b => b.ForumPostId == post.Id && b.UserId == _h.StudentB.Id).Should().Be(0);
    }

    [Fact]
    public async Task Consultant_cannot_bookmark()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.AsConsultant();
        var handler = new ToggleBookmarkCommandHandler(_h.Db, _h.CurrentUser);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(new ToggleBookmarkCommand(post.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Bookmarking_a_reply_throws_conflict()
    {
        var root = await _h.SeedPostAsync(_h.StudentA);
        var reply = await _h.SeedReplyAsync(_h.StudentA, root);
        _h.AsStudent(_h.StudentB);
        var handler = new ToggleBookmarkCommandHandler(_h.Db, _h.CurrentUser);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ToggleBookmarkCommand(reply.Id), CancellationToken.None));
    }
}
