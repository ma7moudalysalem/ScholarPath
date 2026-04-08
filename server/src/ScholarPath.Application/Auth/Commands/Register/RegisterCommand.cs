using MediatR;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    bool RememberMe,
    string? IpAddress,
    string? UserAgent) : IRequest<AuthTokensDto>;
