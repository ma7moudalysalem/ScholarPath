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

namespace ScholarPath.Application.CompanyReviews.Commands.HideCompanyReview;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Admin moderates a company review (FR-075): hides it from public listings or
/// un-hides a previously hidden one. A hidden review is excluded from the company
/// rating average and public review feed but is never deleted — the row and its
/// moderation note are retained for audit.
/// </summary>
[Auditable(AuditAction.Moderated, "CompanyReview",
    TargetIdProperty = nameof(ReviewId),
    SummaryTemplate = "Set company review {ReviewId} hidden state to {Hide}")]
public sealed record HideCompanyReviewCommand(
    Guid ReviewId, bool Hide, string? AdminNote) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class HideCompanyReviewCommandValidator
    : AbstractValidator<HideCompanyReviewCommand>
{
    public HideCompanyReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.AdminNote)
            .MaximumLength(1000)
            .When(x => x.AdminNote is not null);
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class HideCompanyReviewCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<HideCompanyReviewCommandHandler> logger)
    : IRequestHandler<HideCompanyReviewCommand, bool>
{
    public async Task<bool> Handle(HideCompanyReviewCommand request, CancellationToken ct)
    {
        if (!currentUser.IsInRole("Admin"))
            throw new ForbiddenAccessException("Only an administrator can moderate reviews.");

        var review = await db.CompanyReviews
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId && !r.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(CompanyReview), request.ReviewId);

        review.IsHiddenByAdmin = request.Hide;
        review.AdminNote = string.IsNullOrWhiteSpace(request.AdminNote)
            ? null
            : request.AdminNote.Trim();

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Company review {ReviewId} hidden state set to {Hide} by admin {AdminId}.",
            review.Id, request.Hide, currentUser.UserId);
        return true;
    }
}
