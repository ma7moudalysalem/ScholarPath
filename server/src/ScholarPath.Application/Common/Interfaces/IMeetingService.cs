namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Provisions the video-meeting room for a confirmed consultant booking,
/// issues per-participant access tokens, and records the session. Backed by
/// Azure Communication Services in production; a deterministic stub runs when
/// ACS is not configured — the same config-driven fallback pattern as
/// IStripeService and ISsoService.
/// </summary>
public interface IMeetingService
{
    /// <summary>The active provider name ("AzureCommunicationServices" or "Stub").</summary>
    string Provider { get; }

    /// <summary>Creates the meeting room a confirmed booking's session will use.</summary>
    Task<MeetingRoom> CreateRoomAsync(Guid bookingId, CancellationToken ct);

    /// <summary>Issues a participant a short-lived token to join the room.</summary>
    Task<MeetingAccessToken> IssueAccessTokenAsync(string roomId, Guid userId, CancellationToken ct);

    /// <summary>
    /// Starts recording an active call (PB-006). <paramref name="serverCallId"/>
    /// is read from the live call by the client. Returns the provider recording id.
    /// </summary>
    Task<string> StartRecordingAsync(string serverCallId, CancellationToken ct);

    /// <summary>
    /// Downloads a finished recording's bytes from the provider content URL —
    /// used by the recording-ready webhook to move the file into blob storage.
    /// </summary>
    Task<Stream> DownloadRecordingAsync(string contentLocation, CancellationToken ct);
}

/// <summary>A provisioned meeting room.</summary>
public sealed record MeetingRoom(string RoomId);

/// <summary>A participant's credentials for joining a meeting room.</summary>
public sealed record MeetingAccessToken(
    string RoomId,
    string Token,
    string AcsUserId,
    DateTimeOffset ExpiresAt);
