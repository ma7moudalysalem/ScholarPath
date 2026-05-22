using Microsoft.Extensions.Logging.Abstractions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Community.EventHandlers;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;

namespace ScholarPath.UnitTests.Community;

public sealed class ForumReplyCreatedEventHandlerTests : IDisposable
{
    private readonly CommunityTestHarness _h = new();
    private readonly ICommunityRealtimeNotifier _realtime = Substitute.For<ICommunityRealtimeNotifier>();
    private readonly INotificationDispatcher _notifications = Substitute.For<INotificationDispatcher>();
    public void Dispose() => _h.Dispose();

    private ForumReplyCreatedEventHandler NewHandler() =>
        new(_realtime, _h.Db, _notifications,
            NullLogger<ForumReplyCreatedEventHandler>.Instance);

    [Fact]
    public async Task Reply_to_someone_elses_post_dispatches_ReplyOnYourPost_to_parent_author()
    {
        var root = await _h.SeedPostAsync(_h.StudentA);
        var reply = await _h.SeedReplyAsync(_h.StudentB, root);

        await NewHandler().Handle(
            new ForumReplyCreatedEvent(reply.Id, root.Id, _h.StudentB.Id),
            CancellationToken.None);

        await _notifications.Received(1).DispatchAsync(
            _h.StudentA.Id,
            NotificationType.ReplyOnYourPost,
            Arg.Any<NotificationParams>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reply_to_own_post_does_not_dispatch_notification()
    {
        var root = await _h.SeedPostAsync(_h.StudentA);
        var reply = await _h.SeedReplyAsync(_h.StudentA, root);

        await NewHandler().Handle(
            new ForumReplyCreatedEvent(reply.Id, root.Id, _h.StudentA.Id),
            CancellationToken.None);

        await _notifications.DidNotReceive().DispatchAsync(
            Arg.Any<Guid>(),
            NotificationType.ReplyOnYourPost,
            Arg.Any<NotificationParams>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Realtime_emitter_is_called_with_aligned_event_name_via_interface()
    {
        // The interface method name maps 1:1 to the SignalR event name in the
        // SignalR-backed implementation (CommunityRealtimeNotifier). Asserting
        // on the interface call here is the unit-test boundary; the
        // server-side string mapping is exercised by the integration layer.
        var root = await _h.SeedPostAsync(_h.StudentA);
        var reply = await _h.SeedReplyAsync(_h.StudentB, root);

        await NewHandler().Handle(
            new ForumReplyCreatedEvent(reply.Id, root.Id, _h.StudentB.Id),
            CancellationToken.None);

        await _realtime.Received(1).NotifyNewReplyAsync(reply.Id, root.Id, Arg.Any<CancellationToken>());
    }
}
