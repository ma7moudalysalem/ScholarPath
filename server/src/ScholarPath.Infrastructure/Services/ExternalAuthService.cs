using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Infrastructure.Services;

public class ExternalAuthService : IExternalAuthService
{
  private readonly IConfiguration _config;
  private readonly IHostEnvironment _env;

  public ExternalAuthService(IConfiguration config, IHostEnvironment env)
  {
    _config = config;
    _env = env;
  }

  public async Task<ExternalUserInfo?> ValidateAsync(string provider, string providerToken, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerToken))
      return null;

    var p = provider.Trim().ToLowerInvariant();
    var token = providerToken.Trim();

    // DEV tokens (for local Swagger demo)
    if (_env.IsDevelopment())
    {
      if (p == "google" && token == "DEV_GOOGLE_TOKEN")
        return new ExternalUserInfo("Google", "google-dev-123", "tasneem.student@test.com", "Tasneem", "Student", true);

      if ((p == "microsoft" || p == "ms") && token == "DEV_MS_TOKEN")
        return new ExternalUserInfo("Microsoft", "ms-dev-456", "tasneem.student@test.com", "Tasneem", "Student", true);
    }

    return p switch
    {
      "google" => await ValidateGoogleIdToken(token, ct),
      "microsoft" or "ms" => await ValidateMicrosoftIdToken(token, ct),
      _ => null
    };
  }

  private async Task<ExternalUserInfo?> ValidateGoogleIdToken(string idToken, CancellationToken ct)
  {
    var clientId = _config["ExternalAuth:Google:ClientId"];
    if (string.IsNullOrWhiteSpace(clientId)) return null;

    try
    {
      var payload = await GoogleJsonWebSignature.ValidateAsync(
          idToken,
          new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } }
      );

      
      return new ExternalUserInfo(
          "Google",
          payload.Subject,
          payload.Email,
          payload.GivenName,
          payload.FamilyName,
          payload.EmailVerified
      );
    }
    catch
    {
      return null;
    }
  }

  private async Task<ExternalUserInfo?> ValidateMicrosoftIdToken(string idToken, CancellationToken ct)
  {
    var clientId = _config["ExternalAuth:Microsoft:ClientId"];
    var tenantId = _config["ExternalAuth:Microsoft:TenantId"] ?? "common";
    if (string.IsNullOrWhiteSpace(clientId)) return null;

    try
    {
      var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
      var metadataAddress = $"{authority}/.well-known/openid-configuration";

      var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
          metadataAddress,
          new OpenIdConnectConfigurationRetriever()
      );

      var oidc = await configManager.GetConfigurationAsync(ct);

      var parameters = new TokenValidationParameters
      {
        ValidateAudience = true,
        ValidAudience = clientId,
        ValidateIssuer = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2),
        IssuerSigningKeys = oidc.SigningKeys
      };

      var handler = new JwtSecurityTokenHandler();
      var principal = handler.ValidateToken(idToken, parameters, out _);

      var email =
          principal.FindFirst("preferred_username")?.Value ??
          principal.FindFirst(ClaimTypes.Email)?.Value ??
          principal.FindFirst("email")?.Value;

      if (string.IsNullOrWhiteSpace(email)) return null;

      var providerUserId =
          principal.FindFirst("oid")?.Value ??
          principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
          principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
          Guid.NewGuid().ToString("N");

      return new ExternalUserInfo(
          "Microsoft",
          providerUserId,
          email,
          principal.FindFirst("given_name")?.Value,
          principal.FindFirst("family_name")?.Value,
          true
      );
    }
    catch
    {
      return null;
    }
  }
}
