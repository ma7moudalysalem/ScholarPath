using MediatR;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.Commands.Login;

[Auditable(AuditAction.Login, "User")]
public sealed record LoginCommand(
    string Email,
    string Password,
    bool RememberMe,
    string? IpAddress,
    string? UserAgent) : IRequest<AuthTokensDto>;
