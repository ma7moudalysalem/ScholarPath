namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Publishes domain events to an external streaming bus (Azure Event Hubs in
/// production; a no-op stub in development). Handlers call this once per
/// relevant domain event so the stream-analytics pipeline can detect anomalies
/// and feed the Power BI live tile (SRS PB-018 FR-283).
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes <paramref name="payload"/> as a JSON-encoded event with the
    /// given <paramref name="eventType"/> label. The call is fire-and-forget in
    /// the sense that it does not affect the transactional outcome of the
    /// command — failures are logged but NOT re-thrown so a hub outage cannot
    /// break the student / consultant flows.
    /// </summary>
    Task PublishAsync(string eventType, object payload, CancellationToken ct = default);
}
