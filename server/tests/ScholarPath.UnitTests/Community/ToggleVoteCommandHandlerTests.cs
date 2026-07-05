using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Community.Commands.ToggleVote;
using ScholarPath.Domain.Enums;

namespace ScholarPath.UnitTests.Community;

public sealed class ToggleVoteCommandHandlerTests : IDisposable
{
    private readonly CommunityTestHarness _h = new();
    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task Author_cannot_vote_on_own_post()
    {
        // A user cannot vote on their own content (mirrors the self-report block
        // on FlagPost) — the handler rejects it with a ConflictException.
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.AsStudent(_h.StudentA);
        var handler = new ToggleVoteCommandHandler(_h.Db, _h.CurrentUser);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new ToggleVoteCommand(post.Id, VoteType.Up), CancellationToken.None));

        _h.Db.ForumPosts.Single(p => p.Id == post.Id).UpvoteCount.Should().Be(0);
    }

    [Fact]
    public async Task Consultant_cannot_vote()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.AsConsultant();
        var handler = new ToggleVoteCommandHandler(_h.Db, _h.CurrentUser);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(new ToggleVoteCommand(post.Id, VoteType.Up), CancellationToken.None));
    }

    [Fact]
    public async Task Repeated_same_vote_toggles_off()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.AsStudent(_h.StudentB);
        var handler = new ToggleVoteCommandHandler(_h.Db, _h.CurrentUser);

        await handler.Handle(new ToggleVoteCommand(post.Id, VoteType.Up), CancellationToken.None);
        _h.Db.ForumPosts.Single(p => p.Id == post.Id).UpvoteCount.Should().Be(1);

        await handler.Handle(new ToggleVoteCommand(post.Id, VoteType.Up), CancellationToken.None);
        _h.Db.ForumPosts.Single(p => p.Id == post.Id).UpvoteCount.Should().Be(0);
    }
}
