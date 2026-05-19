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
}
