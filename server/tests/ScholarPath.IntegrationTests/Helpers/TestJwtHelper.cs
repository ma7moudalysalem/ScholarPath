using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ScholarPath.IntegrationTests.Helpers;

/// <summary>
/// Generates JWT tokens for integration tests using the same
/// signing key, issuer, and audience as the API (from appsettings.json).
/// </summary>
public static class TestJwtHelper
{
    // Must match appsettings.json → Jwt section exactly
    private const string SigningKey =
        "PLACEHOLDER_JWT_DEV_SIGNING_KEY_AT_LEAST_64_CHARS_FOR_HMAC_SHA256_xxxxx";

    private const string Issuer = "https://scholarpath.local";
    private const string Audience = "https://scholarpath.local";

    public static string GenerateStudentToken(Guid userId)
        => GenerateToken(userId, "Student");

    public static string GenerateConsultantToken(Guid userId)
        => GenerateToken(userId, "Consultant");

    public static string GenerateAdminToken(Guid userId)
        => GenerateToken(userId, "Admin");

    public static string GenerateToken(Guid userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role,           role),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

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
