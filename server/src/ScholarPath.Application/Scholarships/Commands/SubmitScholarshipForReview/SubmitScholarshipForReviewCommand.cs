using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Scholarships.Commands.SubmitScholarshipForReview;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// The owning provider (or an admin) submits a DRAFT scholarship. A provider
/// draft enters the admin moderation queue (UnderReview); an admin-created
/// (ownerless) draft is published directly (Open) — the admin IS the moderator,
/// mirroring the create split. Lets a listing be saved as a draft and finished /
/// submitted later (PB-005 "Save as draft").
/// </summary>
[Auditable(AuditAction.Update, "Scholarship",
    TargetIdProperty = nameof(ScholarshipId),
    SummaryTemplate = "Submitted scholarship {ScholarshipId} for review")]
public sealed record SubmitScholarshipForReviewCommand(Guid ScholarshipId) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class SubmitScholarshipForReviewCommandValidator
    : AbstractValidator<SubmitScholarshipForReviewCommand>
{
    public SubmitScholarshipForReviewCommandValidator()
        => RuleFor(x => x.ScholarshipId).NotEmpty();
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class SubmitScholarshipForReviewCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<SubmitScholarshipForReviewCommandHandler> logger)
    : IRequestHandler<SubmitScholarshipForReviewCommand, bool>
{
    public async Task<bool> Handle(SubmitScholarshipForReviewCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var scholarship = await db.Scholarships
            .FirstOrDefaultAsync(s => s.Id == request.ScholarshipId && !s.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Scholarship), request.ScholarshipId);

        // Only the owning provider may submit their draft; admins may submit any.
        if (scholarship.OwnerScholarshipProviderId != userId
            && !currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("You can only submit your own scholarships.");

        if (scholarship.Status != ScholarshipStatus.Draft)
            throw new ConflictException("Only a draft scholarship can be submitted for review.");

        // Same deadline rule the create flow enforces — a draft may have sat past
        // the 7-day boundary, so re-check before it can go live / enter moderation.
        if (scholarship.Deadline <= DateTimeOffset.UtcNow.AddDays(7))
            throw new ConflictException(
                "Set the application deadline to at least 7 days away before submitting.");

        // Ownerless (admin-created) draft publishes directly; a provider draft
        // enters moderation. Mirrors CreateScholarshipCommand's split.
        var ownerless = scholarship.OwnerScholarshipProviderId is null;
        scholarship.Status = ownerless ? ScholarshipStatus.Open : ScholarshipStatus.UnderReview;
        scholarship.OpenedAt = ownerless ? DateTimeOffset.UtcNow : null;
        scholarship.RejectionReason = null;
        scholarship.RejectedAt = null;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Scholarship {ScholarshipId} submitted for review by {UserId} -> {Status}.",
            scholarship.Id, userId, scholarship.Status);

        return true;
    }
}
