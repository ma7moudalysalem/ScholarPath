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

namespace ScholarPath.Application.Resources.Commands.FeatureResource;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>Admin features / un-features a published resource. Homepage holds up to 6 (PB-009 AC#7).</summary>
[Auditable(AuditAction.Update, "Resource",
    TargetIdProperty = nameof(ResourceId),
    SummaryTemplate = "Set featured={Featured} on resource {ResourceId}")]
public sealed record FeatureResourceCommand(Guid ResourceId, bool Featured) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class FeatureResourceCommandValidator : AbstractValidator<FeatureResourceCommand>
{
    public FeatureResourceCommandValidator() => RuleFor(x => x.ResourceId).NotEmpty();
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class FeatureResourceCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<FeatureResourceCommandHandler> logger)
    : IRequestHandler<FeatureResourceCommand, bool>
{
    private const int MaxFeatured = 6;

    public async Task<bool> Handle(FeatureResourceCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("Only an administrator can feature resources.");

        var resource = await db.Resources
            .FirstOrDefaultAsync(r => r.Id == request.ResourceId, ct)
            ?? throw new NotFoundException(nameof(Resource), request.ResourceId);

        if (request.Featured)
        {
            if (resource.Status != ResourceStatus.Published)
                throw new ConflictException("Only a published resource can be featured.");

            if (!resource.IsFeatured)
            {
                var featuredCount = await db.Resources.CountAsync(r => r.IsFeatured, ct);
                if (featuredCount >= MaxFeatured)
                    throw new ConflictException(
                        $"The homepage already has {MaxFeatured} featured resources. Un-feature one first.");

                var maxOrder = await db.Resources
                    .Where(r => r.IsFeatured)
                    .Select(r => (int?)r.FeaturedOrder)
                    .MaxAsync(ct) ?? 0;

                resource.IsFeatured = true;
                resource.FeaturedOrder = maxOrder + 1;
            }
        }
        else
        {
            resource.IsFeatured = false;
            resource.FeaturedOrder = 0;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Resource {ResourceId} featured={Featured}.", resource.Id, resource.IsFeatured);
        return true;
    }
}
