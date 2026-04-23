using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Commands.ReviewUpgradeRequest;

public enum UpgradeDecision
{
    Approve = 1,
    Reject = 2,
}

/// <summary>
/// Admin decision on an upgrade request (Student → Company or Student → Consultant).
/// Approving grants the target role + flips AccountStatus to Active; rejecting stores
/// the reviewer notes for the user-facing rejection screen.
/// </summary>
[Auditable(AuditAction.Update, "UpgradeRequest",
    TargetIdProperty = nameof(RequestId),
    SummaryTemplate = "Admin {Decision} upgrade request {RequestId}")]
public sealed record ReviewUpgradeRequestCommand(
    Guid RequestId,
    UpgradeDecision Decision,
    string? ReviewerNotes) : IRequest<bool>;
