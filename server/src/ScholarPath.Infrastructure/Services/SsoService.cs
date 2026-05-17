using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Real Google / Microsoft OAuth 2.0 implementation of <see cref="ISsoService"/>.
///
/// The authorization-code flow runs in two HTTP calls per provider:
///   1. POST the code (+ client id/secret + redirect URI) to the provider's
///      token endpoint and read back an access token.
///   2. GET the user's profile (email, name, picture) from the provider's
///      userinfo endpoint with that access token.
///
/// Client id/secret/redirect come from the <c>Authentication:Google</c> /
/// <c>Authentication:Microsoft</c> configuration section. The service is
/// functionally complete — it works the moment real OAuth credentials replace
/// the placeholders in config. Until then the placeholder values simply cause
/// the provider to reject the exchange (a 4xx), which surfaces as a
/// <see cref="ConflictException"/>.
/// </summary>
public sealed class SsoService(
    IHttpClientFactory httpFactory,
    IOptions<AuthenticationOptions> options,
    ILogger<SsoService> logger) : ISsoService
{
    /// <summary>Named <see cref="HttpClient"/> used for every provider call.</summary>
    public const string HttpClientName = "sso";

    private const string GoogleAuthorizeEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private static readonly Uri GoogleTokenEndpoint = new("https://oauth2.googleapis.com/token");
    private static readonly Uri GoogleUserInfoEndpoint = new("https://openidconnect.googleapis.com/v1/userinfo");

    private const string MicrosoftAuthorizeEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
    private static readonly Uri MicrosoftTokenEndpoint = new("https://login.microsoftonline.com/common/oauth2/v2.0/token");
    private static readonly Uri MicrosoftUserInfoEndpoint = new("https://graph.microsoft.com/oidc/userinfo");

    private readonly AuthenticationOptions _opts = options.Value;

    // ── Google ────────────────────────────────────────────────────────────────

    public async Task<SsoUserInfo> ExchangeGoogleCodeAsync(string code, string redirectUri, CancellationToken ct)
    {
        var provider = Require(_opts.Google, "Google");
        var effectiveRedirect = ResolveRedirectUri(provider, redirectUri);

        var token = await ExchangeCodeForTokenAsync(
            GoogleTokenEndpoint, provider, code, effectiveRedirect, "Google", ct).ConfigureAwait(false);

        var profile = await FetchUserInfoAsync(
            GoogleUserInfoEndpoint, token.AccessToken!, "Google", ct).ConfigureAwait(false);

        return BuildUserInfo(profile, "Google");
    }

    public string BuildGoogleAuthorizeUrl(string redirectUri, string state)
    {
        var provider = Require(_opts.Google, "Google");
        var effectiveRedirect = ResolveRedirectUri(provider, redirectUri);
        var query = BuildAuthorizeQuery(provider.ClientId!, effectiveRedirect, state, "openid email profile");
        return $"{GoogleAuthorizeEndpoint}?{query}";
    }

    // ── Microsoft ─────────────────────────────────────────────────────────────

    public async Task<SsoUserInfo> ExchangeMicrosoftCodeAsync(string code, string redirectUri, CancellationToken ct)
    {
        var provider = Require(_opts.Microsoft, "Microsoft");
        var effectiveRedirect = ResolveRedirectUri(provider, redirectUri);

        var token = await ExchangeCodeForTokenAsync(
            MicrosoftTokenEndpoint, provider, code, effectiveRedirect, "Microsoft", ct).ConfigureAwait(false);

        var profile = await FetchUserInfoAsync(
            MicrosoftUserInfoEndpoint, token.AccessToken!, "Microsoft", ct).ConfigureAwait(false);

        return BuildUserInfo(profile, "Microsoft");
    }

    public string BuildMicrosoftAuthorizeUrl(string redirectUri, string state)
    {
        var provider = Require(_opts.Microsoft, "Microsoft");
        var effectiveRedirect = ResolveRedirectUri(provider, redirectUri);
        var query = BuildAuthorizeQuery(provider.ClientId!, effectiveRedirect, state, "openid email profile");
        return $"{MicrosoftAuthorizeEndpoint}?{query}";
    }

    // ── Shared OAuth steps ────────────────────────────────────────────────────

    private async Task<OAuthTokenResponse> ExchangeCodeForTokenAsync(
        Uri tokenEndpoint,
        ExternalAuthProviderOptions provider,
        string code,
        string redirectUri,
        string providerName,
        CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = provider.ClientId!,
            ["client_secret"] = provider.ClientSecret!,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
        };

        var client = httpFactory.CreateClient(HttpClientName);
        using var content = new FormUrlEncodedContent(form);

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(tokenEndpoint, content, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "[sso] {Provider} token endpoint unreachable.", providerName);
            throw new ConflictException($"{providerName} sign-in is temporarily unavailable.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            logger.LogWarning(
                "[sso] {Provider} token exchange failed: {Status} {Body}",
                providerName, (int)response.StatusCode, body);
            throw new ConflictException($"{providerName} authorization code could not be exchanged.");
        }

        var token = await response.Content
            .ReadFromJsonAsync<OAuthTokenResponse>(cancellationToken: ct)
            .ConfigureAwait(false);

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            logger.LogWarning("[sso] {Provider} token response had no access_token.", providerName);
            throw new ConflictException($"{providerName} did not return an access token.");
        }

        return token;
    }

    private async Task<OpenIdUserInfo> FetchUserInfoAsync(
        Uri userInfoEndpoint,
        string accessToken,
        string providerName,
        CancellationToken ct)
    {
        var client = httpFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "[sso] {Provider} userinfo endpoint unreachable.", providerName);
            throw new ConflictException($"{providerName} sign-in is temporarily unavailable.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            logger.LogWarning(
                "[sso] {Provider} userinfo failed: {Status} {Body}",
                providerName, (int)response.StatusCode, body);
            throw new ConflictException($"{providerName} profile could not be retrieved.");
        }

        var profile = await response.Content
            .ReadFromJsonAsync<OpenIdUserInfo>(cancellationToken: ct)
            .ConfigureAwait(false);

        if (profile is null || string.IsNullOrWhiteSpace(profile.Email))
        {
            logger.LogWarning("[sso] {Provider} userinfo returned no email.", providerName);
            throw new ConflictException($"{providerName} account did not expose a verified email address.");
        }

        return profile;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SsoUserInfo BuildUserInfo(OpenIdUserInfo profile, string providerName)
    {
        // Prefer the structured given/family names; fall back to splitting the
        // display name so we always have something for first/last.
        var first = profile.GivenName;
        var last = profile.FamilyName;

        if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last)
            && !string.IsNullOrWhiteSpace(profile.Name))
        {
            var parts = profile.Name.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            first = parts.Length > 0 ? parts[0] : null;
            last = parts.Length > 1 ? parts[1] : null;
        }

        var providerUserId = !string.IsNullOrWhiteSpace(profile.Subject)
            ? profile.Subject!
            : profile.Email!;

        return new SsoUserInfo(
            Email: profile.Email!.Trim(),
            FirstName: string.IsNullOrWhiteSpace(first) ? null : first,
            LastName: string.IsNullOrWhiteSpace(last) ? null : last,
            ProfileImageUrl: string.IsNullOrWhiteSpace(profile.Picture) ? null : profile.Picture,
            Provider: providerName,
            ProviderUserId: providerUserId);
    }

    private static string BuildAuthorizeQuery(string clientId, string redirectUri, string state, string scope)
        => string.Join('&',
            $"client_id={Uri.EscapeDataString(clientId)}",
            "response_type=code",
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}",
            $"scope={Uri.EscapeDataString(scope)}",
            $"state={Uri.EscapeDataString(state)}",
            "access_type=offline",
            "prompt=select_account");

    private static string ResolveRedirectUri(ExternalAuthProviderOptions provider, string callerRedirectUri)
        => !string.IsNullOrWhiteSpace(provider.RedirectUri)
            ? provider.RedirectUri!
            : callerRedirectUri;

    private static ExternalAuthProviderOptions Require(ExternalAuthProviderOptions provider, string providerName)
    {
        if (string.IsNullOrWhiteSpace(provider.ClientId) || string.IsNullOrWhiteSpace(provider.ClientSecret))
            throw new ConflictException($"{providerName} SSO is not configured.");
        return provider;
    }

    // ── Provider response shapes ──────────────────────────────────────────────

    private sealed record OAuthTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }

    private sealed record OpenIdUserInfo
    {
        [JsonPropertyName("sub")]
        public string? Subject { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; init; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; init; }

        [JsonPropertyName("picture")]
        public string? Picture { get; init; }
    }
}
