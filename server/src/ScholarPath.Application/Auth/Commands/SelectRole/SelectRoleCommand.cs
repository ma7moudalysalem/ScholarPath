using MediatR;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.Commands.SelectRole;

/// <summary>
/// One-time first-role selection for a freshly-registered Unassigned account (Task 5A).
/// Student is granted immediately; Company/Consultant enter the admin onboarding queue.
/// </summary>
[Auditable(AuditAction.RoleChanged, "User")]
public sealed record SelectRoleCommand(string Role) : IRequest<AuthTokensDto>;
