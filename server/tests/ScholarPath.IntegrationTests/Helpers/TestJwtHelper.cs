using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace ScholarPath.IntegrationTests.Helpers;

/// <summary>
/// Generates RS256 JWT tokens for integration tests. The tokens are signed with
/// the API's own RSA signing key — obtained from the running host's
/// <c>IJwtKeyProvider</c> — so they validate against the real
/// <c>AddJwtBearer</c> pipeline. Issuer and audience match the API config.
/// </summary>
public static class TestJwtHelper
{
    // Must match appsettings.json → Jwt section.
    private const string Issuer = "https://scholarpath.local";
    private const string Audience = "https://scholarpath.local";

    public static string GenerateStudentToken(RsaSecurityKey signingKey, Guid userId)
        => GenerateToken(signingKey, userId, "Student");

    public static string GenerateConsultantToken(RsaSecurityKey signingKey, Guid userId)
        => GenerateToken(signingKey, userId, "Consultant");

    public static string GenerateAdminToken(RsaSecurityKey signingKey, Guid userId)
        => GenerateToken(signingKey, userId, "Admin");

    public static string GenerateToken(RsaSecurityKey signingKey, Guid userId, string role)
    {
        ArgumentNullException.ThrowIfNull(signingKey);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role,           role),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
