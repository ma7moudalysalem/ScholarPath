using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Commands.ApproveOnboarding;

public enum OnboardingDecision
{
    Approve = 1,
    Reject = 2,
}

/// <summary>
/// Admin review of a user sitting in AccountStatus.PendingApproval
/// (company/consultant onboarding gate — FR-018). Approving flips to Active;
/// rejecting moves the user to Deactivated and surfaces the reason on their
/// next login so they know what to fix.
/// </summary>
[Auditable(AuditAction.Update, "User",
    TargetIdProperty = nameof(UserId),
    SummaryTemplate = "Admin {Decision} onboarding for user {UserId}")]
public sealed record ReviewOnboardingCommand(
    Guid UserId,
    OnboardingDecision Decision,
    string? ReviewerNotes) : IRequest<bool>;
