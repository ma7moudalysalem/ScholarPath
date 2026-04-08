using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Common.Interfaces;

public interface ITokenService
{
    TokenPair IssueTokens(ApplicationUser user, IEnumerable<string> roles, string? activeRole, bool rememberMe);
    Task<TokenPair?> RotateRefreshTokenAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken ct);
    Task RevokeRefreshTokenAsync(string refreshToken, string? reason, CancellationToken ct);
    Task RevokeAllForUserAsync(Guid userId, string? reason, CancellationToken ct);
}

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt, DateTimeOffset RefreshTokenExpiresAt);

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}

public interface ISsoService
{
    Task<SsoUserInfo> ExchangeGoogleCodeAsync(string code, string redirectUri, CancellationToken ct);
    Task<SsoUserInfo> ExchangeMicrosoftCodeAsync(string code, string redirectUri, CancellationToken ct);
    string BuildGoogleAuthorizeUrl(string redirectUri, string state);
    string BuildMicrosoftAuthorizeUrl(string redirectUri, string state);
}

public sealed record SsoUserInfo(string Email, string? FirstName, string? LastName, string? ProfileImageUrl, string Provider, string ProviderUserId);

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}

public sealed record EmailMessage(string To, string Subject, string HtmlBody, string? TextBody = null, string? ReplyTo = null);

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, string container, CancellationToken ct);
    Task DeleteAsync(string blobUrl, CancellationToken ct);
    Task<Stream> DownloadAsync(string blobUrl, CancellationToken ct);
}
