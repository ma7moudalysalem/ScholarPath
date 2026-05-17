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

namespace ScholarPath.Application.ConsultantBookings.Commands.HideConsultantReview;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Admin moderates a consultant review (FR-101): hides it from public listings or
/// un-hides a previously hidden one. A hidden review is excluded from the consultant
/// rating average (and the auto-suspension window) and the public review feed, but
/// is never deleted — the row and its moderation note are retained for audit.
/// </summary>
[Auditable(AuditAction.Moderated, "ConsultantReview",
    TargetIdProperty = nameof(ReviewId),
    SummaryTemplate = "Set consultant review {ReviewId} hidden state to {Hide}")]
public sealed record HideConsultantReviewCommand(
    Guid ReviewId, bool Hide, string? AdminNote) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class HideConsultantReviewCommandValidator
    : AbstractValidator<HideConsultantReviewCommand>
{
    public HideConsultantReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.AdminNote)
            .MaximumLength(1000)
            .When(x => x.AdminNote is not null);
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class HideConsultantReviewCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<HideConsultantReviewCommandHandler> logger)
    : IRequestHandler<HideConsultantReviewCommand, bool>
{
    public async Task<bool> Handle(HideConsultantReviewCommand request, CancellationToken ct)
    {
        if (!currentUser.IsInRole("Admin"))
            throw new ForbiddenAccessException("Only an administrator can moderate reviews.");

        var review = await db.ConsultantReviews
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId && !r.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ConsultantReview), request.ReviewId);

        review.IsHiddenByAdmin = request.Hide;
        review.AdminNote = string.IsNullOrWhiteSpace(request.AdminNote)
            ? null
            : request.AdminNote.Trim();

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Consultant review {ReviewId} hidden state set to {Hide} by admin {AdminId}.",
            review.Id, request.Hide, currentUser.UserId);
        return true;
    }
}
