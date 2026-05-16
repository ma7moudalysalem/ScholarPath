using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.Commands.Logout;

[Auditable(AuditAction.Logout, "User")]
public sealed record LogoutCommand(string RefreshToken) : IRequest;
