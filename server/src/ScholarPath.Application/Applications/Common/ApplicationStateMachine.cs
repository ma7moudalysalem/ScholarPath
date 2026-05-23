using ScholarPath.Domain.Enums;
using ScholarPath.Application.Common.Exceptions;

namespace ScholarPath.Application.Applications.Common;

/// <summary>
/// Defines all valid ApplicationTracker status transitions.
/// Single source of truth for state-machine logic across all command handlers.
/// </summary>
public static class ApplicationStateMachine
{
    /// <summary>
    /// Returns true if the transition from <paramref name="from"/> to <paramref name="to"/> is permitted.
    /// </summary>
    public static bool IsTransitionAllowed(ApplicationStatus from, ApplicationStatus to) => (from, to) switch
    {
        // Normal in-app flow
        (ApplicationStatus.Draft, ApplicationStatus.Pending) => true,
        (ApplicationStatus.Pending, ApplicationStatus.UnderReview) => true,
        // A company may also decide directly on a Pending application without an
        // explicit "move to under review" step — clicking Accept / Reject /
        // Shortlist on a freshly-submitted application is the common path and
        // previously threw "Transition from Pending to Accepted is not allowed"
        // (QA BUG-018).
        (ApplicationStatus.Pending, ApplicationStatus.Shortlisted) => true,
        (ApplicationStatus.Pending, ApplicationStatus.Accepted) => true,
        (ApplicationStatus.Pending, ApplicationStatus.Rejected) => true,
        (ApplicationStatus.UnderReview, ApplicationStatus.Shortlisted) => true,
        (ApplicationStatus.UnderReview, ApplicationStatus.Accepted) => true,
        (ApplicationStatus.UnderReview, ApplicationStatus.Rejected) => true,
        (ApplicationStatus.Shortlisted, ApplicationStatus.Accepted) => true,
        (ApplicationStatus.Shortlisted, ApplicationStatus.Rejected) => true,

        // Withdrawal is allowed from any non-terminal state
        (ApplicationStatus.Draft or
         ApplicationStatus.Pending or
         ApplicationStatus.UnderReview or
         ApplicationStatus.Shortlisted, ApplicationStatus.Withdrawn) => true,

        _ => false
    };

    /// <summary>
    /// Throws <see cref="ConflictException"/> if the transition is not permitted.
    /// </summary>
    public static void EnsureTransition(ApplicationStatus from, ApplicationStatus to)
    {
        if (!IsTransitionAllowed(from, to))
            throw new ConflictException(
                $"Transition from '{from}' to '{to}' is not allowed.");
    }
}
