using Microsoft.Extensions.Logging.Abstractions;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Community.Commands.FlagPost;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.UnitTests.Community;

public sealed class FlagPostCommandHandlerTests : IDisposable
{
    private readonly CommunityTestHarness _h = new();
    private readonly INotificationDispatcher _notifications = Substitute.For<INotificationDispatcher>();
    public void Dispose() => _h.Dispose();

    // Each handler call gets a fresh context over the same in-memory store.
    // This avoids the EF InMemory provider's tracking quirk where modifying
    // an entity AND adding a child in a single SaveChanges raises a phantom
    // "entity does not exist" error if the entity was previously saved in
    // the same context.
    private FlagPostCommandHandler NewHandler(out IApplicationDbContext ctx)
    {
        ctx = _h.NewContext();
        return new FlagPostCommandHandler(
            ctx, _h.CurrentUser, _notifications,
            NullLogger<FlagPostCommandHandler>.Instance);
    }

    [Fact]
    public async Task Consultant_cannot_flag()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.AsConsultant();
        var handler = NewHandler(out _);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(new FlagPostCommand(post.Id, "spam", null), CancellationToken.None));
    }

    // The next two tests exercise the FlagPost SaveChanges path. The handler now
    // adds the flag via the ForumFlags DbSet (not the post.Flags navigation) and
    // derives the distinct-flag count from a query, so the InMemory provider no
    // longer trips the phantom-navigation error that previously forced these to be
    // skipped. Prod concurrency (rowversion collisions between simultaneous flags)
    // is handled by the retry loop in the handler.

    [Fact]
    public async Task Duplicate_flag_from_same_user_is_blocked()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);
        _h.AsStudent(_h.StudentB);

        var first = NewHandler(out _);
        await first.Handle(new FlagPostCommand(post.Id, "spam", null), CancellationToken.None);

        var second = NewHandler(out _);
        await Assert.ThrowsAsync<ConflictException>(() =>
            second.Handle(new FlagPostCommand(post.Id, "spam", null), CancellationToken.None));
    }

    [Fact]
    public async Task Three_distinct_flags_auto_hide_the_post()
    {
        var post = await _h.SeedPostAsync(_h.StudentA);

        var studentC = NewStudent("studentc@test.local");
        var studentD = NewStudent("studentd@test.local");

        foreach (var reporter in new[] { _h.StudentB, studentC, studentD })
        {
            _h.AsStudent(reporter);
            var handler = NewHandler(out _);
            await handler.Handle(new FlagPostCommand(post.Id, "spam", null), CancellationToken.None);
        }

        // Re-read with a fresh context to see the persisted state.
        using var verify = _h.NewContext();
        var fresh = verify.ForumPosts.Single(p => p.Id == post.Id);
        fresh.IsAutoHidden.Should().BeTrue();
        fresh.ModerationStatus.Should().Be(PostModerationStatus.PendingReview);
    }

    private ApplicationUser NewStudent(string email)
    {
        var u = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = "Student",
            LastName = email,
            ActiveRole = "Student",
            AccountStatus = AccountStatus.Active,
        };
        _h.Db.Users.Add(u);
        _h.Db.SaveChanges();
        _h.Db.ChangeTracker.Clear();
        return u;
    }
}
