namespace ScholarPath.Application.Auth.DTOs;

public record ExternalLoginRequest(string Provider, string ProviderToken);

public record LinkProviderRequest(string Provider, string ProviderToken);
