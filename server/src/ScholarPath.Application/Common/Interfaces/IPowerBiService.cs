namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Power BI embed-token DTO returned by <see cref="IPowerBiService"/>.
/// </summary>
/// <param name="IsConfigured">False when the Power BI workspace is not provisioned yet.</param>
/// <param name="Token">Short-lived embed token (null when not configured).</param>
/// <param name="EmbedUrl">iframe src URL for the report (null when not configured).</param>
/// <param name="ReportId">Report GUID (null when not configured).</param>
/// <param name="ExpiresAt">UTC expiry of the embed token (null when not configured).</param>
public sealed record EmbedTokenDto(
    bool IsConfigured,
    string? Token,
    string? EmbedUrl,
    string? ReportId,
    DateTimeOffset? ExpiresAt);

/// <summary>
/// Service that mints a short-lived Power BI embed token scoped to the
/// requesting user's identity and role (PB-015 T-014).
/// </summary>
public interface IPowerBiService
{
    /// <summary>
    /// Returns an <see cref="EmbedTokenDto"/> with <c>IsConfigured = false</c>
    /// when the Power BI workspace is not provisioned, otherwise returns a
    /// valid short-lived embed token scoped to <paramref name="userEmail"/>
    /// and <paramref name="activeRole"/> via Power BI RLS.
    /// </summary>
    Task<EmbedTokenDto> GetEmbedTokenAsync(
        string reportType,
        Guid userId,
        string userEmail,
        string activeRole,
        CancellationToken ct);
}
