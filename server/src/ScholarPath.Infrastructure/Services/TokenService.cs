using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

public sealed class TokenService(
    IOptions<JwtOptions> jwtOptions,
    ApplicationDbContext db,
    IDateTimeService clock,
    ILogger<TokenService> logger) : ITokenService
{
    private readonly JwtOptions _opts = jwtOptions.Value;

    public TokenPair IssueTokens(ApplicationUser user, IEnumerable<string> roles, string? activeRole, bool rememberMe)
    {
        var now = clock.UtcNow;
        var accessExpires = now.AddMinutes(_opts.AccessTokenExpirationMinutes);
        var refreshExpires = rememberMe
            ? now.AddDays(_opts.RefreshTokenRememberMeExpirationDays)
            : now.AddDays(_opts.RefreshTokenExpirationDays);

        var accessToken = CreateAccessToken(user, roles, activeRole, accessExpires);
        var refreshToken = GenerateSecureRefreshToken();

        var refreshHash = HashRefreshToken(refreshToken);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExpires,
            CreatedAt = now,
        });
        db.SaveChanges();

        return new TokenPair(accessToken, refreshToken, accessExpires, refreshExpires);
    }

    public async Task<TokenPair?> RotateRefreshTokenAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        var hash = HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct).ConfigureAwait(false);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt < clock.UtcNow || stored.User is null)
        {
            logger.LogWarning("Refresh token invalid or expired");
            return null;
        }

        // Mark as revoked and create a replacement
        stored.IsRevoked = true;
        stored.RevokedAt = clock.UtcNow;
        stored.RevokedReason = "Rotated";

        // Issue fresh pair and link as replacement
        var roles = new List<string>(); // role fetching lives in handler; keep empty here
        var pair = IssueTokens(stored.User, roles, stored.User.ActiveRole, rememberMe: false);
        var newHash = HashRefreshToken(pair.RefreshToken);
        var newToken = await db.RefreshTokens.FirstAsync(t => t.TokenHash == newHash, ct).ConfigureAwait(false);
        stored.ReplacedByTokenId = newToken.Id;
        stored.IpAddress = ipAddress;
        stored.UserAgent = userAgent;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return pair;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, string? reason, CancellationToken ct)
    {
        var hash = HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct).ConfigureAwait(false);
        if (stored is null || stored.IsRevoked) return;

        stored.IsRevoked = true;
        stored.RevokedAt = clock.UtcNow;
        stored.RevokedReason = reason;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RevokeAllForUserAsync(Guid userId, string? reason, CancellationToken ct)
    {
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(ct).ConfigureAwait(false);

        foreach (var t in tokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = clock.UtcNow;
            t.RevokedReason = reason;
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private string CreateAccessToken(ApplicationUser user, IEnumerable<string> roles, string? activeRole, DateTimeOffset expires)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("account_status", user.AccountStatus.ToString()),
            new("onboarding_complete", user.IsOnboardingComplete.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };
        if (!string.IsNullOrEmpty(activeRole))
        {
            claims.Add(new Claim("active_role", activeRole));
        }
        foreach (var r in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, r));
        }

        // For the scaffold we use HMAC-SHA256 with the SigningKey from options.
        // Production: swap to RS256 with Azure Key Vault private key.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateSecureRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashRefreshToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
