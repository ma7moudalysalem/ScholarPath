using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Community.Queries.GetPostDetails;
using ScholarPath.Domain.Entities;

namespace ScholarPath.UnitTests.Community;

/// <summary>
/// FR-MSG-29: a mutual block hides the blocked author's whole community thread from
/// the viewer — in EITHER block direction — while leaving unrelated threads visible.
/// </summary>
public sealed class GetPostDetailsBlockTests : IDisposable
{
    private readonly CommunityTestHarness _h = new();
    public void Dispose() => _h.Dispose();

    private GetPostDetailsQueryHandler Sut() => new(_h.Db, _h.CurrentUser);

    [Fact]
    public async Task Thread_is_hidden_when_viewer_blocked_the_author()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.Db.UserBlocks.Add(new UserBlock { BlockerId = _h.StudentB.Id, BlockedUserId = _h.StudentA.Id });
        await _h.Db.SaveChangesAsync();
        _h.AsStudent(_h.StudentB);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Sut().Handle(new GetPostDetailsQuery(post.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Thread_is_hidden_when_the_author_blocked_the_viewer()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.Db.UserBlocks.Add(new UserBlock { BlockerId = _h.StudentA.Id, BlockedUserId = _h.StudentB.Id });
        await _h.Db.SaveChangesAsync();
        _h.AsStudent(_h.StudentB);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Sut().Handle(new GetPostDetailsQuery(post.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Thread_is_visible_when_there_is_no_block()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.AsStudent(_h.StudentB);

        var result = await Sut().Handle(new GetPostDetailsQuery(post.Id), CancellationToken.None);

        result.Post.Id.Should().Be(post.Id);
    }
}
