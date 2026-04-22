using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Commands.ChangeUserRole;

public enum RoleOp
{
    Add = 1,
    Remove = 2,
}

/// <summary>
/// Admin adds or removes a role from a user. Scoped to the six roles the
/// platform defines (Student / Company / Consultant / Admin / SuperAdmin / Moderator).
/// Anything outside that whitelist is rejected by the validator.
/// </summary>
[Auditable(AuditAction.Update, "User",
    TargetIdProperty = nameof(UserId),
    SummaryTemplate = "Admin {Operation} role '{Role}' on user {UserId}")]
public sealed record ChangeUserRoleCommand(
    Guid UserId,
    string Role,
    RoleOp Operation) : IRequest<bool>;
