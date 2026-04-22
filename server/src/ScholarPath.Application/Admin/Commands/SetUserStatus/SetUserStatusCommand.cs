using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Commands.SetUserStatus;

/// <summary>
/// Admin command to activate / suspend / deactivate a user account.
/// Suspended and Deactivated statuses automatically revoke active refresh tokens
/// (see <see cref="IUserAdministration"/>).
/// </summary>
[Auditable(AuditAction.Update, "User",
    TargetIdProperty = nameof(UserId),
    SummaryTemplate = "Admin set account status to {NewStatus} for user {UserId}")]
public sealed record SetUserStatusCommand(
    Guid UserId,
    AccountStatus NewStatus,
    string? Reason) : IRequest<bool>;
