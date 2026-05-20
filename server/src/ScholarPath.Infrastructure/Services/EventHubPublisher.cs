using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Azure Event Hubs publisher (PB-018 T-003). Wraps an
/// <see cref="EventHubProducerClient"/> created from the configured connection
/// string and hub name.
///
/// Failures are caught and logged but NOT re-thrown — a transient hub outage
/// must not roll back a student's application, booking, or payment. The
/// stream-analytics pipeline is opportunistic; a missed event is recovered by
/// the next successful event rather than by blocking the OLTP path.
///
/// The producer client is created per-publish call (lightweight and thread-safe
/// for the EventHubs SDK) so the service can safely be registered as a singleton.
/// </summary>
public sealed class EventHubPublisher(
    IOptions<EventHubOptions> opts,
    ILogger<EventHubPublisher> logger) : IEventPublisher
{
    private readonly EventHubOptions _opts = opts.Value;

    public async Task PublishAsync(string eventType, object payload, CancellationToken ct = default)
    {
        try
        {
            var envelope = new
            {
                eventType,
                occurredAt = DateTimeOffset.UtcNow,
                source = "ScholarPath.API",
                data = payload,
            };
            var json = JsonSerializer.Serialize(envelope);
            var data = new EventData(Encoding.UTF8.GetBytes(json));
            data.Properties["eventType"] = eventType;

            await using var producer = new EventHubProducerClient(
                _opts.ConnectionString,
                _opts.HubName);

            using var batch = await producer.CreateBatchAsync(ct).ConfigureAwait(false);
            if (!batch.TryAdd(data))
            {
                logger.LogWarning(
                    "EventHub: event {EventType} too large for a single batch; skipped.",
                    eventType);
                return;
            }

            await producer.SendAsync(batch, ct).ConfigureAwait(false);
            logger.LogDebug("EventHub: published {EventType}.", eventType);
        }
        catch (OperationCanceledException)
        {
            throw; // Let the caller's cancellation propagate normally.
        }
        catch (Exception ex)
        {
            // Swallow all other exceptions: a hub failure must not roll back
            // the surrounding business transaction.
            logger.LogError(ex, "EventHub: failed to publish {EventType}; continuing.", eventType);
        }
    }
}
