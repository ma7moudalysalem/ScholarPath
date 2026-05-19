using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Deterministic offline meeting provider used when Azure Communication
/// Services is not configured (dev, tests, and any environment without an ACS
/// connection string). Mirrors <c>StubStripeService</c> / <c>StubSsoService</c>:
/// the booking and no-show flows stay fully exercised without a cloud resource.
/// </summary>
public sealed class StubMeetingService : IMeetingService
{
    public string Provider => "Stub";

    public Task<MeetingRoom> CreateRoomAsync(Guid bookingId, CancellationToken ct)
        => Task.FromResult(new MeetingRoom($"stub-room-{bookingId:N}"));

    public Task<MeetingAccessToken> IssueAccessTokenAsync(
        string roomId, Guid userId, CancellationToken ct)
        => Task.FromResult(new MeetingAccessToken(
            RoomId: roomId,
            Token: $"stub-token-{userId:N}",
            AcsUserId: $"stub-user-{userId:N}",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(2)));

    public Task<string> StartRecordingAsync(string serverCallId, CancellationToken ct)
        => Task.FromResult($"stub-recording-{Guid.NewGuid():N}");

    public Task<Stream> DownloadRecordingAsync(string contentLocation, CancellationToken ct)
        => Task.FromResult<Stream>(new MemoryStream());
}
