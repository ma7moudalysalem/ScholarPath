using FluentAssertions;
using ScholarPath.Infrastructure.Hubs;
using Xunit;

namespace ScholarPath.UnitTests.Hubs;

/// <summary>
/// Covers <see cref="PresenceTracker"/> — the ref-counted online-user tracker
/// behind the chat hub's UserOnline / UserOffline presence broadcasts.
/// </summary>
public sealed class PresenceTrackerTests
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";

    [Fact]
    public void Connect_FirstConnectionForAUser_ReportsTheUserCameOnline()
    {
        var tracker = new PresenceTracker();

        tracker.Connect(UserA).Should().BeTrue();
    }

    [Fact]
    public void Connect_AdditionalConnectionsForTheSameUser_DoNotRepeatTheOnlineSignal()
    {
        var tracker = new PresenceTracker();
        tracker.Connect(UserA);

        // A second tab / a reconnect must not re-announce an already-online user.
        tracker.Connect(UserA).Should().BeFalse();
        tracker.Connect(UserA).Should().BeFalse();
    }

    [Fact]
    public void Disconnect_WhileOtherConnectionsRemain_DoesNotReportOffline()
    {
        var tracker = new PresenceTracker();
        tracker.Connect(UserA);
        tracker.Connect(UserA); // two live connections

        tracker.Disconnect(UserA).Should().BeFalse();
    }

    [Fact]
    public void Disconnect_LastConnection_ReportsTheUserWentOffline()
    {
        var tracker = new PresenceTracker();
        tracker.Connect(UserA);
        tracker.Connect(UserA);
        tracker.Disconnect(UserA);

        tracker.Disconnect(UserA).Should().BeTrue();
    }

    [Fact]
    public void Disconnect_UnknownUser_IsANoOp()
    {
        var tracker = new PresenceTracker();

        tracker.Disconnect(UserA).Should().BeFalse();
    }

    [Fact]
    public void OnlineUsers_ListsOnlyUsersWithAtLeastOneLiveConnection()
    {
        var tracker = new PresenceTracker();
        tracker.Connect(UserA);
        tracker.Connect(UserB);
        tracker.Connect(UserB);

        tracker.OnlineUsers().Should().BeEquivalentTo(UserA, UserB);

        tracker.Disconnect(UserA); // A's only connection drops

        tracker.OnlineUsers().Should().BeEquivalentTo(new[] { UserB });
    }

    [Fact]
    public void Connect_AfterAFullDisconnect_ReportsOnlineAgain()
    {
        var tracker = new PresenceTracker();
        tracker.Connect(UserA);
        tracker.Disconnect(UserA);

        // A returning user must be announced online a second time.
        tracker.Connect(UserA).Should().BeTrue();
    }
}
