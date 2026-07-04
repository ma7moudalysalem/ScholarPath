using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Scholarships.Commands.ApproveScholarship;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>Admin approves an under-review scholarship — moves it to <c>Open</c>.</summary>
[Auditable(AuditAction.Approved, "Scholarship",
    TargetIdProperty = nameof(ScholarshipId),
    SummaryTemplate = "Approved scholarship {ScholarshipId}")]
public sealed record ApproveScholarshipCommand(Guid ScholarshipId) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class ApproveScholarshipCommandValidator : AbstractValidator<ApproveScholarshipCommand>
{
    public ApproveScholarshipCommandValidator() => RuleFor(x => x.ScholarshipId).NotEmpty();
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class ApproveScholarshipCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<ApproveScholarshipCommandHandler> logger)
    : IRequestHandler<ApproveScholarshipCommand, bool>
{
    public async Task<bool> Handle(ApproveScholarshipCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("Only an administrator can approve scholarships.");

        var adminId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var scholarship = await db.Scholarships
            .FirstOrDefaultAsync(s => s.Id == request.ScholarshipId && !s.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Scholarship), request.ScholarshipId);

        if (scholarship.Status != ScholarshipStatus.UnderReview)
            throw new ConflictException("Only a scholarship under review can be approved.");

        // FR-SCH-22: the 7-day deadline rule applies at PUBLISHING too, not just
        // on create/update. A listing that sat in moderation until its deadline
        // is within 7 days (or has passed) must not go Open — the provider has
        // to extend the deadline (via edit) and resubmit first.
        if (scholarship.Deadline <= DateTimeOffset.UtcNow.AddDays(7))
            throw new ConflictException(
                "Cannot approve: the application deadline must be at least 7 days away. Ask the provider to extend the deadline and resubmit.");

        scholarship.Status = ScholarshipStatus.Open;
        scholarship.OpenedAt ??= DateTimeOffset.UtcNow;
        // Clear any stale rejection feedback now that it's approved.
        scholarship.RejectionReason = null;
        scholarship.RejectedAt = null;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Notify the owning provider (US-SCH-13). Admin-created (ownerless)
        // listings have no provider to tell.
        if (scholarship.OwnerScholarshipProviderId is { } ownerId)
        {
            await notifications.DispatchAsync(
                ownerId,
                NotificationType.ScholarshipApproved,
                new NotificationParams { TitleEn = scholarship.TitleEn, TitleAr = scholarship.TitleAr },
                deepLink: "/company/scholarships",
                idempotencyKey: $"scholarship-approved:{scholarship.Id}",
                ct).ConfigureAwait(false);
        }

        logger.LogInformation("Scholarship {ScholarshipId} approved by {AdminId}.", scholarship.Id, adminId);
        return true;
    }
}
