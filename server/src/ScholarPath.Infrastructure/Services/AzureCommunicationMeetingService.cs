using Azure.Communication;
using Azure.Communication.Identity;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Azure Communication Services meeting provider — selected when
/// <c>Acs:ConnectionString</c> is configured. A booking's "room" is an ACS
/// group-call id (a GUID): no server-side room object is needed, the id is
/// only handed to the two booking participants through the authorized join
/// endpoint, and each participant gets a short-lived VoIP-scoped token minted
/// from the ACS resource.
/// </summary>
public sealed class AzureCommunicationMeetingService : IMeetingService
{
    private readonly CommunicationIdentityClient _identityClient;

    public AzureCommunicationMeetingService(IOptions<AcsOptions> options)
    {
        _identityClient = new CommunicationIdentityClient(options.Value.ConnectionString);
    }

    public string Provider => "AzureCommunicationServices";

    public Task<MeetingRoom> CreateRoomAsync(Guid bookingId, CancellationToken ct)
        => Task.FromResult(new MeetingRoom(Guid.NewGuid().ToString()));

    public async Task<MeetingAccessToken> IssueAccessTokenAsync(
        string roomId, Guid userId, CancellationToken ct)
    {
        var result = await _identityClient
            .CreateUserAndTokenAsync(new[] { CommunicationTokenScope.VoIP }, ct)
            .ConfigureAwait(false);

        return new MeetingAccessToken(
            RoomId: roomId,
            Token: result.Value.AccessToken.Token,
            AcsUserId: result.Value.User.Id,
            ExpiresAt: result.Value.AccessToken.ExpiresOn);
    }
}
