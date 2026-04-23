using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Commands.ApproveOnboarding;

public sealed class ReviewOnboardingCommandHandler(
    IApplicationDbContext db,
    IUserAdministration admin,
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

        logger.LogInformation("Onboarding {Decision} for {UserId} → {Status}. Notes: {Notes}",
            request.Decision, request.UserId, newStatus, request.ReviewerNotes ?? "<none>");

        return true;
    }
}
