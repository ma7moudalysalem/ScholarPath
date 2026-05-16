using MediatR;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.Commands.SwitchRole;

[Auditable(AuditAction.RoleChanged, "User")]
public sealed record SwitchRoleCommand(string TargetRole) : IRequest<AuthTokensDto>;
