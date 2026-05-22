using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Commands.ApproveOnboarding;

public sealed class ReviewOnboardingCommandHandler(
    IApplicationDbContext db,
    IUserAdministration admin,
    INotificationDispatcher notifications,
    ILogger<ReviewOnboardingCommandHandler> logger)
    : IRequestHandler<ReviewOnboardingCommand, bool>
{
    public async Task<bool> Handle(ReviewOnboardingCommand request, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("User", request.UserId);

        if (user.AccountStatus != AccountStatus.PendingApproval)
        {
            throw new ConflictException(
                $"User {request.UserId} is in status {user.AccountStatus}; onboarding review only applies to PendingApproval.");
        }

        // FR-152: a rejected applicant returns to Unassigned so they can correct
        // their details and resubmit onboarding, rather than being locked out
        // permanently. The rejection reason reaches them via the notification below
        // and is also stored on UserProfile so the wizard can re-surface it on
        // resubmission (AUTH-CODE-06 / FR-ONB-07).
        var newStatus = request.Decision == OnboardingDecision.Approve
            ? AccountStatus.Active
            : AccountStatus.Unassigned;

        var ok = await admin.SetAccountStatusAsync(
            request.UserId, newStatus, request.ReviewerNotes, ct).ConfigureAwait(false);

        if (!ok) throw new NotFoundException("User", request.UserId);

        // On approval, grant the Identity role the user requested at onboarding
        // (carried on ActiveRole) and mark onboarding complete — without this the
        // user would end up Active with no role at all.
        if (request.Decision == OnboardingDecision.Approve)
        {
            if (!string.IsNullOrWhiteSpace(user.ActiveRole))
                await admin.AddRoleAsync(request.UserId, user.ActiveRole, ct).ConfigureAwait(false);

            user.IsOnboardingComplete = true;
            // Approval clears any stale rejection feedback so a future onboarding
            // change wouldn't accidentally re-surface an old reason.
            if (user.Profile is not null)
            {
                user.Profile.LastOnboardingRejectionReason = null;
                user.Profile.LastOnboardingRejectedAt = null;
            }
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        else
        {
            // Persist the rejection feedback for the applicant to see when they
            // resubmit. We only write the reason when one was actually supplied —
            // null/whitespace would just clear the previous one for no reason.
            if (user.Profile is not null && !string.IsNullOrWhiteSpace(request.ReviewerNotes))
            {
                user.Profile.LastOnboardingRejectionReason = request.ReviewerNotes;
                user.Profile.LastOnboardingRejectedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
        }

        // Tell the applicant their onboarding was decided (in-app + email).
        await notifications.DispatchAsync(
            request.UserId,
            request.Decision == OnboardingDecision.Approve
                ? NotificationType.OnboardingApproved
                : NotificationType.OnboardingRejected,
            new NotificationParams { Reason = request.ReviewerNotes },
            deepLink: null,
            idempotencyKey: $"onboarding-review:{request.UserId:N}",
            ct).ConfigureAwait(false);

        logger.LogInformation("Onboarding {Decision} for {UserId} → {Status}. Notes: {Notes}",
            request.Decision, request.UserId, newStatus, request.ReviewerNotes ?? "<none>");

        return true;
    }
}
