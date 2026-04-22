using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Commands.SoftDeleteUser;

/// <summary>
/// Admin soft-delete. Marks the user deleted, flips status to Deactivated,
/// revokes every refresh token. PII scrubbing is deferred to the data-delete
/// job so the admin trail stays legible; this command is the immediate kill-switch.
/// </summary>
[Auditable(AuditAction.Delete, "User",
    TargetIdProperty = nameof(UserId),
    SummaryTemplate = "Admin soft-deleted user {UserId}")]
public sealed record SoftDeleteUserCommand(Guid UserId, string? Reason) : IRequest<bool>;
