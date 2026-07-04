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

namespace ScholarPath.Application.Scholarships.Commands.RejectScholarship;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Admin rejects an under-review scholarship — sends it back to <c>Draft</c> so the
/// owning company can revise and resubmit. The reason is captured in the audit log.
/// </summary>
[Auditable(AuditAction.Rejected, "Scholarship",
    TargetIdProperty = nameof(ScholarshipId),
    SummaryTemplate = "Rejected scholarship {ScholarshipId}")]
public sealed record RejectScholarshipCommand(Guid ScholarshipId, string Reason) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class RejectScholarshipCommandValidator : AbstractValidator<RejectScholarshipCommand>
{
    public RejectScholarshipCommandValidator()
    {
        RuleFor(x => x.ScholarshipId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A rejection reason is required.")
            .MaximumLength(2000);
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class RejectScholarshipCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<RejectScholarshipCommandHandler> logger)
    : IRequestHandler<RejectScholarshipCommand, bool>
{
    public async Task<bool> Handle(RejectScholarshipCommand request, CancellationToken ct)
    {
        if (!currentUser.IsInRole("Admin"))
            throw new ForbiddenAccessException("Only an administrator can reject scholarships.");

        var adminId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var scholarship = await db.Scholarships
            .FirstOrDefaultAsync(s => s.Id == request.ScholarshipId && !s.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Scholarship), request.ScholarshipId);

        if (scholarship.Status != ScholarshipStatus.UnderReview)
            throw new ConflictException("Only a scholarship under review can be rejected.");

        scholarship.Status = ScholarshipStatus.Draft;
        scholarship.RejectionReason = request.Reason;
        scholarship.RejectedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Notify the owning provider with the reason (US-SCH-13). Ownerless
        // (admin-created) listings have no provider to tell.
        if (scholarship.OwnerScholarshipProviderId is { } ownerId)
        {
            await notifications.DispatchAsync(
                ownerId,
                NotificationType.ScholarshipRejected,
                new NotificationParams { TitleEn = scholarship.TitleEn, TitleAr = scholarship.TitleAr, Reason = request.Reason },
                deepLink: "/company/scholarships",
                idempotencyKey: $"scholarship-rejected:{scholarship.Id}:{scholarship.RejectedAt:O}",
                ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Scholarship {ScholarshipId} rejected by {AdminId}. Reason: {Reason}",
            scholarship.Id, adminId, request.Reason);
        return true;
    }
}
