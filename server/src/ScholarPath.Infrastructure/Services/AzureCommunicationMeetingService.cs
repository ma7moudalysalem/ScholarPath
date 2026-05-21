using Azure.Communication;
using Azure.Communication.CallAutomation;
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
/// from the ACS resource. Sessions are recorded via Call Automation.
/// </summary>
public sealed class AzureCommunicationMeetingService : IMeetingService
{
    private readonly CommunicationIdentityClient _identityClient;
    private readonly CallAutomationClient _callAutomationClient;

    public AzureCommunicationMeetingService(IOptions<AcsOptions> options)
    {
        var connectionString = options.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Acs:ConnectionString is required to use AzureCommunicationMeetingService. "
                + "Configure it via the Acs__ConnectionString App Service setting, or omit it to "
                + "fall back to StubMeetingService.");
        }
        _identityClient = new CommunicationIdentityClient(connectionString);
        _callAutomationClient = new CallAutomationClient(connectionString);
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

    public async Task<string> StartRecordingAsync(string serverCallId, CancellationToken ct)
    {
        var result = await _callAutomationClient.GetCallRecording()
            .StartAsync(new StartRecordingOptions(new ServerCallLocator(serverCallId)), ct)
            .ConfigureAwait(false);
        return result.Value.RecordingId;
    }

    public async Task<Stream> DownloadRecordingAsync(string contentLocation, CancellationToken ct)
    {
        var result = await _callAutomationClient.GetCallRecording()
            .DownloadStreamingAsync(new Uri(contentLocation), cancellationToken: ct)
            .ConfigureAwait(false);
        return result.Value;
    }
}
