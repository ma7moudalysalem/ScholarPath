using MediatR;
using ScholarPath.Application.Common.Attributes;

namespace ScholarPath.Application.Auth.Commands.Logout;

[Auditable(AuditAction.Logout, "User")]
public record LogoutCommand(
    string RefreshToken,
    bool LogoutEverywhere = false) : IRequest<Unit>;
