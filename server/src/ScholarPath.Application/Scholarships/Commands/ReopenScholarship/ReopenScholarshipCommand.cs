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
