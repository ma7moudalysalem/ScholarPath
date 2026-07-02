using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Community.Commands.CreateReply;

namespace ScholarPath.UnitTests.Community;

public sealed class CreateReplyCommandHandlerTests : IDisposable
{
    private readonly CommunityTestHarness _h = new();
    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task Student_can_reply_and_parent_reply_count_increments()
    {
        var root = await _h.SeedPostAsync(_h.StudentA);
        _h.AsStudent(_h.StudentB);
        var handler = new CreateReplyCommandHandler(_h.Db, _h.CurrentUser);

        var replyId = await handler.Handle(
            new CreateReplyCommand(root.Id, "this is a reply"),
            CancellationToken.None);

        var reply = _h.Db.ForumPosts.Single(p => p.Id == replyId);
        reply.AuthorId.Should().Be(_h.StudentB.Id);
        reply.ParentPostId.Should().Be(root.Id);

        var updatedRoot = _h.Db.ForumPosts.Single(p => p.Id == root.Id);
        updatedRoot.ReplyCount.Should().Be(1);
    }

    [Fact]
    public async Task Consultant_cannot_reply()
    {
        var root = await _h.SeedPostAsync(_h.StudentA);
        _h.AsConsultant();
        var handler = new CreateReplyCommandHandler(_h.Db, _h.CurrentUser);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(new CreateReplyCommand(root.Id, "no"), CancellationToken.None));
    }

    [Fact]
    public async Task ScholarshipProvider_cannot_reply()
    {
        var root = await _h.SeedPostAsync(_h.StudentA);
        _h.AsScholarshipProvider();
        var handler = new CreateReplyCommandHandler(_h.Db, _h.CurrentUser);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(new CreateReplyCommand(root.Id, "no"), CancellationToken.None));
    }
}
