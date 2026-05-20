using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Real Power BI embed-token service (PB-015 T-014).
///
/// Auth flow:
///   1. Acquire an Azure AD access token for the service principal via
///      client_credentials grant (scope = https://analysis.windows.net/powerbi/api/.default).
///   2. POST to the Power BI GenerateToken endpoint for the requested report,
///      embedding the caller's identity for RLS.
///
/// The service is stateless — token caching is left to the caller (the
/// <c>AnalyticsEmbedded</c> frontend page refreshes the token every 4 hours).
/// </summary>
public sealed class PowerBiService(
    IHttpClientFactory httpFactory,
    IOptions<PowerBiOptions> opts,
    ILogger<PowerBiService> logger) : IPowerBiService
{
    private readonly PowerBiOptions _opts = opts.Value;

    private const string PowerBiScope = "https://analysis.windows.net/powerbi/api/.default";

    public async Task<EmbedTokenDto> GetEmbedTokenAsync(
        string reportType,
        Guid userId,
        string userEmail,
        string activeRole,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opts.WorkspaceId))
            return new EmbedTokenDto(IsConfigured: false, null, null, null, null);

        if (!_opts.ReportIds.TryGetValue(reportType, out var reportId)
            || string.IsNullOrWhiteSpace(reportId))
        {
            logger.LogWarning("PowerBi: no report ID configured for type '{ReportType}'.", reportType);
            return new EmbedTokenDto(IsConfigured: false, null, null, null, null);
        }

        // 1. Acquire Azure AD access token for the service principal.
        var accessToken = await AcquireAadTokenAsync(ct);

        // 2. Call Power BI GenerateToken.
        using var client = httpFactory.CreateClient("PowerBi");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var embedUrl = $"https://app.powerbi.com/reportEmbed?reportId={reportId}&groupId={_opts.WorkspaceId}";

        var body = new
        {
            accessLevel = "View",
            identities  = string.IsNullOrWhiteSpace(_opts.DatasetId)
                ? null
                : new[]
                {
                    new
                    {
                        username = userEmail,
                        roles    = new[] { activeRole },
                        datasets = new[] { _opts.DatasetId },
                    },
                },
        };

        var response = await client.PostAsJsonAsync(
            $"https://api.powerbi.com/v1.0/myorg/groups/{_opts.WorkspaceId}/reports/{reportId}/GenerateToken",
            body, ct).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PowerBiTokenResponse>(
            cancellationToken: ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty response from Power BI GenerateToken.");

        logger.LogInformation(
            "PowerBi: issued embed token for {ReportType} (expires {Expiry:u}).",
            reportType, result.Expiration);

        return new EmbedTokenDto(
            IsConfigured: true,
            Token:        result.Token,
            EmbedUrl:     embedUrl,
            ReportId:     reportId,
            ExpiresAt:    result.Expiration);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task<string> AcquireAadTokenAsync(CancellationToken ct)
    {
        using var client = httpFactory.CreateClient();

        var tokenUrl = new Uri(
            $"https://login.microsoftonline.com/{_opts.TenantId}/oauth2/v2.0/token");

        var form = new Dictionary<string, string>
        {
            ["grant_type"]    = "client_credentials",
            ["client_id"]     = _opts.ServicePrincipalClientId,
            ["client_secret"] = _opts.ServicePrincipalClientSecret,
            ["scope"]         = PowerBiScope,
        };

        using var formContent = new FormUrlEncodedContent(form);
        var response = await client.PostAsync(tokenUrl, formContent, ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var aad = await response.Content.ReadFromJsonAsync<AadTokenResponse>(
            cancellationToken: ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty AAD token response.");

        return aad.AccessToken;
    }

    // ── JSON response shapes ──────────────────────────────────────────────

    private sealed record PowerBiTokenResponse(
        [property: JsonPropertyName("token")]      string Token,
        [property: JsonPropertyName("expiration")] DateTimeOffset Expiration);

    private sealed record AadTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken);
}
