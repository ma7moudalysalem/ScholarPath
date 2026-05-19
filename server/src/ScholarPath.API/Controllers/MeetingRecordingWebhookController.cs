using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.ConsultantBookings.Commands.StoreSessionRecording;

namespace ScholarPath.API.Controllers;

/// <summary>
/// Receives Azure Event Grid events for ACS call recordings (PB-006). Handles
/// the Event Grid subscription-validation handshake and, when a recording file
/// is ready, moves it into blob storage. Anonymous — Event Grid is not an
/// authenticated user; the endpoint only acts on recognised ACS event types.
/// </summary>
[ApiController]
[Route("api/meeting-recording")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class MeetingRecordingWebhookController(
    IMediator mediator,
    ILogger<MeetingRecordingWebhookController> logger) : ControllerBase
{
    [HttpPost("events")]
    public async Task<IActionResult> Events(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(body))
        {
            return Ok();
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        using (doc)
        {
            var events = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.EnumerateArray().ToList()
                : [doc.RootElement];

            foreach (var evt in events)
            {
                var eventType = GetString(evt, "eventType") ?? GetString(evt, "type");

                // Event Grid subscription handshake — echo the validation code.
                if (eventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
                {
                    var code = evt.TryGetProperty("data", out var vd)
                        ? GetString(vd, "validationCode")
                        : null;
                    return Ok(new { validationResponse = code });
                }

                if (eventType == "Microsoft.Communication.RecordingFileStatusUpdated")
                {
                    await ProcessRecordingEventAsync(evt, ct).ConfigureAwait(false);
                }
            }
        }

        return Ok();
    }

    private async Task ProcessRecordingEventAsync(JsonElement evt, CancellationToken ct)
    {
        var recordingId = RecordingIdFromSubject(GetString(evt, "subject"));
        if (recordingId is null)
        {
            logger.LogWarning("Recording event without a parseable recordingId in the subject.");
            return;
        }

        if (!evt.TryGetProperty("data", out var data)
            || !data.TryGetProperty("recordingStorageInfo", out var storageInfo)
            || !storageInfo.TryGetProperty("recordingChunks", out var chunks)
            || chunks.ValueKind != JsonValueKind.Array)
        {
            logger.LogWarning("Recording {RecordingId} event carried no recording chunks.", recordingId);
            return;
        }

        foreach (var chunk in chunks.EnumerateArray())
        {
            var contentLocation = GetString(chunk, "contentLocation");
            if (string.IsNullOrWhiteSpace(contentLocation))
            {
                continue;
            }

            try
            {
                await mediator
                    .Send(new StoreSessionRecordingCommand(recordingId, contentLocation), ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to store recording {RecordingId}.", recordingId);
            }
        }
    }

    private static string? GetString(JsonElement el, string name)
        => el.ValueKind == JsonValueKind.Object
           && el.TryGetProperty(name, out var v)
           && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    /// <summary>Subject is "/recording/call/{serverCallId}/recordingId/{recordingId}".</summary>
    private static string? RecordingIdFromSubject(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        var parts = subject.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var idx = Array.IndexOf(parts, "recordingId");
        return idx >= 0 && idx + 1 < parts.Length ? parts[idx + 1] : null;
    }
}
