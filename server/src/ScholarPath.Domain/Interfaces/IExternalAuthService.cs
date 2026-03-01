namespace ScholarPath.Domain.Interfaces;

public record ExternalUserInfo(
    string Provider,        
    string ProviderUserId,
    string Email,
    string? FirstName,
    string? LastName,
    bool EmailVerified);

public interface IExternalAuthService
{
  Task<ExternalUserInfo?> ValidateAsync(string provider, string providerToken, CancellationToken ct);
}
