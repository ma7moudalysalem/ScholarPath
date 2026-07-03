namespace ScholarPath.Infrastructure.Settings;

/// <summary>
/// Azure Communication Services configuration (video meetings). When
/// <see cref="ConnectionString"/> is unset the app falls back to the
/// deterministic <c>StubMeetingService</c>.
/// </summary>
public sealed class AcsOptions
{
    public const string SectionName = "Acs";

    /// <summary>ACS resource connection string — <c>endpoint=https://...;accesskey=...</c>.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// SEC-05 — pre-shared secret required as the <c>?code=</c> query token on the
    /// Event Grid recording webhook URL (<c>POST api/meeting-recording/events?code=...</c>).
    /// Event Grid is not an authenticated user, so this token is how the anonymous
    /// endpoint authenticates the caller. Configure the SAME value here and on the
    /// Event Grid subscription endpoint URL. Production sets it via the
    /// <c>Acs__WebhookKey</c> App Service setting (or Key Vault) — never commit a
    /// real value. When empty the webhook is rejected outside Development.
    /// </summary>
    public string WebhookKey { get; set; } = string.Empty;
}
