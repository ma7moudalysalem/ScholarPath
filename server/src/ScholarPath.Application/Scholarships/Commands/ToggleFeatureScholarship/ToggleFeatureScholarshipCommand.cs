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

namespace ScholarPath.Application.Scholarships.Commands.ToggleFeatureScholarship;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Admin features or un-features a scholarship. The homepage holds up to 12
/// featured scholarships (PB-003 FR-030). A scholarship must be <c>Open</c>
/// before it can be featured.
/// </summary>
[Auditable(AuditAction.Update, "Scholarship",
    TargetIdProperty = nameof(ScholarshipId),
    SummaryTemplate = "Set featured={Featured} on scholarship {ScholarshipId}")]
public sealed record ToggleFeatureScholarshipCommand(Guid ScholarshipId, bool Featured) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class ToggleFeatureScholarshipCommandValidator
    : AbstractValidator<ToggleFeatureScholarshipCommand>
{
    public ToggleFeatureScholarshipCommandValidator()
        => RuleFor(x => x.ScholarshipId).NotEmpty();
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class ToggleFeatureScholarshipCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<ToggleFeatureScholarshipCommandHandler> logger)
    : IRequestHandler<ToggleFeatureScholarshipCommand, bool>
{
    private const int MaxFeatured = 12;

    public async Task<bool> Handle(
        ToggleFeatureScholarshipCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("Only an administrator can feature scholarships.");

        var scholarship = await db.Scholarships
            .FirstOrDefaultAsync(s => s.Id == request.ScholarshipId && !s.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Scholarship), request.ScholarshipId);

        if (request.Featured)
        {
            if (scholarship.Status != ScholarshipStatus.Open)
                throw new ConflictException("Only an Open scholarship can be featured.");

            if (!scholarship.IsFeatured)
            {
                var featuredCount = await db.Scholarships
                    .CountAsync(s => s.IsFeatured && !s.IsDeleted, ct);
                if (featuredCount >= MaxFeatured)
                    throw new ConflictException(
                        $"The homepage already has {MaxFeatured} featured scholarships. Un-feature one first.");

                var maxOrder = await db.Scholarships
                    .Where(s => s.IsFeatured && !s.IsDeleted)
                    .Select(s => (int?)s.FeaturedOrder)
                    .MaxAsync(ct) ?? 0;

                scholarship.IsFeatured   = true;
                scholarship.FeaturedOrder = maxOrder + 1;
            }
        }
        else
        {
            scholarship.IsFeatured    = false;
            scholarship.FeaturedOrder = 0;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Scholarship {ScholarshipId} featured={Featured}.",
            scholarship.Id, scholarship.IsFeatured);
        return true;
    }
}
