namespace ScholarPath.Infrastructure.Hubs;

/// <summary>
/// Tracks which users currently hold at least one live SignalR connection.
/// Registered as a singleton so presence is shared across every hub instance
/// (hubs are created per-invocation). Connections are ref-counted per user, so
/// a second browser tab — or an automatic reconnect — never produces a false
/// "offline" while another connection is still live.
/// </summary>
public interface IPresenceTracker
{
    /// <summary>
    /// Records a new connection for <paramref name="userId"/>. Returns
    /// <see langword="true"/> only when this is the user's first live
    /// connection — i.e. they just transitioned to online.
    /// </summary>
    bool Connect(string userId);

    /// <summary>
    /// Records a dropped connection for <paramref name="userId"/>. Returns
    /// <see langword="true"/> only when this was the user's last live
    /// connection — i.e. they just transitioned to offline.
    /// </summary>
    bool Disconnect(string userId);

    /// <summary>A snapshot of every user with at least one live connection.</summary>
    IReadOnlyList<string> OnlineUsers();
}

/// <inheritdoc />
public sealed class PresenceTracker : IPresenceTracker
{
    private readonly object _gate = new();
    private readonly Dictionary<string, int> _connectionCounts = new();

    public bool Connect(string userId)
    {
        lock (_gate)
        {
            if (_connectionCounts.TryGetValue(userId, out var count))
            {
                _connectionCounts[userId] = count + 1;
                return false;
            }

            _connectionCounts[userId] = 1;
            return true;
        }
    }

    public bool Disconnect(string userId)
    {
        lock (_gate)
        {
            if (!_connectionCounts.TryGetValue(userId, out var count))
            {
                return false;
            }

            if (count <= 1)
            {
                _connectionCounts.Remove(userId);
                return true;
            }

            _connectionCounts[userId] = count - 1;
            return false;
        }
    }

    public IReadOnlyList<string> OnlineUsers()
    {
        lock (_gate)
        {
            return _connectionCounts.Keys.ToList();
        }
    }
}
