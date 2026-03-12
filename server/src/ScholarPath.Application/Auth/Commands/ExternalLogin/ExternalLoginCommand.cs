using MediatR;
using ScholarPath.Application.Auth.DTOs;

public record ExternalLoginCommand(string Provider, string IdToken, string? ProviderKey = null) : IRequest<AuthResult>;
