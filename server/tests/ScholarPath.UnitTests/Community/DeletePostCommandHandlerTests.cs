using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Community.Commands.DeletePost;

namespace ScholarPath.UnitTests.Community;

public sealed class DeletePostCommandHandlerTests : IDisposable
{
    private readonly CommunityTestHarness _h = new();
    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task Author_can_soft_delete_own_post()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.AsStudent(_h.StudentA);
        var handler = new DeletePostCommandHandler(_h.Db, _h.CurrentUser);

        var ok = await handler.Handle(new DeletePostCommand(post.Id), CancellationToken.None);

        ok.Should().BeTrue();
        var saved = _h.Db.ForumPosts.IgnoreQueryFilters().Single(p => p.Id == post.Id);
        saved.IsDeleted.Should().BeTrue();
        saved.DeletedByUserId.Should().Be(_h.StudentA.Id);
    }

    [Fact]
    public async Task Non_author_student_cannot_delete_someone_elses_post()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.AsStudent(_h.StudentB);
        var handler = new DeletePostCommandHandler(_h.Db, _h.CurrentUser);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(new DeletePostCommand(post.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Author_deleting_reply_decrements_parent_reply_count()
    {
        var root = await _h.SeedPostAsync(_h.StudentA);
        var reply = await _h.SeedReplyAsync(_h.StudentA, root);
        var initialCount = _h.Db.ForumPosts.Single(p => p.Id == root.Id).ReplyCount;

        _h.AsStudent(_h.StudentA);
        var handler = new DeletePostCommandHandler(_h.Db, _h.CurrentUser);
        await handler.Handle(new DeletePostCommand(reply.Id), CancellationToken.None);

        var updatedRoot = _h.Db.ForumPosts.Single(p => p.Id == root.Id);
        updatedRoot.ReplyCount.Should().Be(initialCount - 1);
    }

    [Fact]
    public async Task Admin_can_delete_someone_elses_post()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.AsAdmin();
        var handler = new DeletePostCommandHandler(_h.Db, _h.CurrentUser);

        var ok = await handler.Handle(new DeletePostCommand(post.Id), CancellationToken.None);
        ok.Should().BeTrue();
    }
}
