using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ScholarshipProviderReviews.Commands.HideScholarshipProviderReview;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Admin moderates a company review (FR-075): hides it from public listings or
/// un-hides a previously hidden one. A hidden review is excluded from the company
/// rating average and public review feed but is never deleted — the row and its
/// moderation note are retained for audit.
/// </summary>
[Auditable(AuditAction.Moderated, "ScholarshipProviderReview",
    TargetIdProperty = nameof(ReviewId),
    SummaryTemplate = "Set company review {ReviewId} hidden state to {Hide}")]
public sealed record HideScholarshipProviderReviewCommand(
    Guid ReviewId, bool Hide, string? AdminNote) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class HideScholarshipProviderReviewCommandValidator
    : AbstractValidator<HideScholarshipProviderReviewCommand>
{
    public HideScholarshipProviderReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.AdminNote)
            .MaximumLength(1000)
            .When(x => x.AdminNote is not null);
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class HideScholarshipProviderReviewCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<HideScholarshipProviderReviewCommandHandler> logger)
    : IRequestHandler<HideScholarshipProviderReviewCommand, bool>
{
    public async Task<bool> Handle(HideScholarshipProviderReviewCommand request, CancellationToken ct)
    {
        if (!currentUser.IsInRole("Admin"))
            throw new ForbiddenAccessException("Only an administrator can moderate reviews.");

        var review = await db.ScholarshipProviderReviews
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId && !r.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ScholarshipProviderReview), request.ReviewId);

        review.IsHiddenByAdmin = request.Hide;
        review.AdminNote = string.IsNullOrWhiteSpace(request.AdminNote)
            ? null
            : request.AdminNote.Trim();

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "ScholarshipProvider review {ReviewId} hidden state set to {Hide} by admin {AdminId}.",
            review.Id, request.Hide, currentUser.UserId);
        return true;
    }
}
