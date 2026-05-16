using MediatR;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.Commands.SsoLogin;

/// <summary>
/// Handles a Google / Microsoft OAuth callback: exchanges the code, then
/// finds-or-creates the user by email and issues a token pair.
/// </summary>
[Auditable(AuditAction.Login, "User")]
public sealed record SsoLoginCommand(
    string Provider,
    string Code,
    string RedirectUri) : IRequest<AuthTokensDto>;
