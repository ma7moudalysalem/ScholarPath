namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Read-side view over which users currently hold a live chat-hub connection.
/// Lets Application-layer notification handlers skip email-spam when the
/// recipient is already watching the conversation in real time (PB-007).
/// </summary>
public interface IChatPresenceQuery
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="userId"/> has at
    /// least one live chat-hub connection right now.
    /// </summary>
    bool IsOnline(Guid userId);
}
