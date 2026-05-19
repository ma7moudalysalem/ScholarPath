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
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("User", request.UserId);

        if (user.AccountStatus != AccountStatus.PendingApproval)
        {
            throw new ConflictException(
                $"User {request.UserId} is in status {user.AccountStatus}; onboarding review only applies to PendingApproval.");
        }

        var newStatus = request.Decision == OnboardingDecision.Approve
            ? AccountStatus.Active
            : AccountStatus.Deactivated;

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
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
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
