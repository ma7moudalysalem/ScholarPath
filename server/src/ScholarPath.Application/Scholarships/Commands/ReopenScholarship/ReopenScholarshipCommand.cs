using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Scholarships.Commands.ReopenScholarship;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// The owning provider (or an admin) reopens a CLOSED scholarship. It re-enters
/// the admin moderation queue (UnderReview) exactly like a freshly-submitted
/// listing, so it is never made public again without re-approval (PB-005).
/// </summary>
[Auditable(AuditAction.Update, "Scholarship",
    TargetIdProperty = nameof(ScholarshipId),
    SummaryTemplate = "Reopened scholarship {ScholarshipId}")]
public sealed record ReopenScholarshipCommand(Guid ScholarshipId) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class ReopenScholarshipCommandValidator : AbstractValidator<ReopenScholarshipCommand>
{
    public ReopenScholarshipCommandValidator() => RuleFor(x => x.ScholarshipId).NotEmpty();
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class ReopenScholarshipCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<ReopenScholarshipCommandHandler> logger)
    : IRequestHandler<ReopenScholarshipCommand, bool>
{
    public async Task<bool> Handle(ReopenScholarshipCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var scholarship = await db.Scholarships
            .FirstOrDefaultAsync(s => s.Id == request.ScholarshipId && !s.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Scholarship), request.ScholarshipId);

        // Only the owning provider may reopen their listing; admins may reopen any
        // (they own the platform-created external listings).
        if (scholarship.OwnerScholarshipProviderId != userId
            && !currentUser.IsInRole("Admin") && !currentUser.IsInRole("SuperAdmin"))
            throw new ForbiddenAccessException("You can only reopen your own scholarships.");

        if (scholarship.Status != ScholarshipStatus.Closed)
            throw new ConflictException("Only a closed scholarship can be reopened.");

        // FR-SCH-22: reopening is the most common past-deadline path (listings
        // usually auto-close because the deadline passed). Require a valid
        // ≥7-day deadline before it can re-enter moderation, so it can't be
        // reopened straight into another immediate auto-close.
        if (scholarship.Deadline <= DateTimeOffset.UtcNow.AddDays(7))
            throw new ConflictException(
                "Cannot reopen: update the application deadline to at least 7 days away first.");

        scholarship.Status = ScholarshipStatus.UnderReview;
        scholarship.RejectionReason = null;
        scholarship.RejectedAt = null;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Scholarship {ScholarshipId} reopened by {UserId} — back to moderation.",
            scholarship.Id, userId);

        return true;
    }
}
