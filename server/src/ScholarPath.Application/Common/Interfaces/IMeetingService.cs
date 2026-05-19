namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Provisions the video-meeting room for a confirmed consultant booking and
/// issues per-participant access tokens. Backed by Azure Communication
/// Services in production; a deterministic stub runs when ACS is not
/// configured — the same config-driven fallback pattern as IStripeService
/// and ISsoService.
/// </summary>
public interface IMeetingService
{
    /// <summary>The active provider name ("AzureCommunicationServices" or "Stub").</summary>
    string Provider { get; }

    /// <summary>Creates the meeting room a confirmed booking's session will use.</summary>
    Task<MeetingRoom> CreateRoomAsync(Guid bookingId, CancellationToken ct);

    /// <summary>Issues a participant a short-lived token to join the room.</summary>
    Task<MeetingAccessToken> IssueAccessTokenAsync(string roomId, Guid userId, CancellationToken ct);
}

/// <summary>A provisioned meeting room.</summary>
public sealed record MeetingRoom(string RoomId);

/// <summary>A participant's credentials for joining a meeting room.</summary>
public sealed record MeetingAccessToken(
    string RoomId,
    string Token,
    string AcsUserId,
    DateTimeOffset ExpiresAt);
