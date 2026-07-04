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

namespace ScholarPath.Application.Resources.Commands.SetResourceVisibility;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Admin moderates a live resource's visibility (PB-009 US-093): Published, Hidden,
/// or Removed. The review workflow (submit/approve/reject) handles pre-publish states.
/// </summary>
[Auditable(AuditAction.Moderated, "Resource",
    TargetIdProperty = nameof(ResourceId),
    SummaryTemplate = "Set resource {ResourceId} visibility to {Status}")]
public sealed record SetResourceVisibilityCommand(
    Guid ResourceId, ResourceStatus Status) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class SetResourceVisibilityCommandValidator
    : AbstractValidator<SetResourceVisibilityCommand>
{
    public SetResourceVisibilityCommandValidator()
    {
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.Status)
            .Must(s => s is ResourceStatus.Published
                        or ResourceStatus.Hidden
                        or ResourceStatus.Removed)
            .WithMessage("Visibility must be Published, Hidden, or Removed.");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class SetResourceVisibilityCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<SetResourceVisibilityCommandHandler> logger)
    : IRequestHandler<SetResourceVisibilityCommand, bool>
{
    public async Task<bool> Handle(SetResourceVisibilityCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("Only an administrator can moderate resources.");

        var resource = await db.Resources
            .FirstOrDefaultAsync(r => r.Id == request.ResourceId, ct)
            ?? throw new NotFoundException(nameof(Resource), request.ResourceId);

        if (resource.Status is not (ResourceStatus.Published
                                    or ResourceStatus.Hidden
                                    or ResourceStatus.Removed))
            throw new ConflictException(
                "Use the review workflow for a resource that has not been published yet.");

        resource.Status = request.Status;

        // A non-public resource cannot stay on the featured homepage rail.
        if (request.Status != ResourceStatus.Published)
        {
            resource.IsFeatured = false;
            resource.FeaturedOrder = 0;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Resource {ResourceId} visibility set to {Status}.", resource.Id, request.Status);
        return true;
    }
}
