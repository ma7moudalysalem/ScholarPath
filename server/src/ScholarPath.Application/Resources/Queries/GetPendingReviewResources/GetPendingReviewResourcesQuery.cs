using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Resources.Queries.GetPendingReviewResources;

/// <summary>Admin review queue — resources awaiting approval, oldest first (PB-009 AC#3).</summary>
public sealed record GetPendingReviewResourcesQuery : IRequest<IReadOnlyList<ResourceListItemDto>>;

public sealed class GetPendingReviewResourcesQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetPendingReviewResourcesQuery, IReadOnlyList<ResourceListItemDto>>
{
    public async Task<IReadOnlyList<ResourceListItemDto>> Handle(
        GetPendingReviewResourcesQuery request, CancellationToken ct)
    {
        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("Only an administrator can view the review queue.");

        var entities = await db.Resources.AsNoTracking()
            .Where(r => r.Status == ResourceStatus.PendingReview)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(ResourceMapping.ToListItem).ToList();
    }
}
