using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Resources.Queries.GetFeaturedResources;

/// <summary>Up to six featured published resources for the homepage hub (PB-009 AC#7).</summary>
public sealed record GetFeaturedResourcesQuery : IRequest<IReadOnlyList<ResourceListItemDto>>;

public sealed class GetFeaturedResourcesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetFeaturedResourcesQuery, IReadOnlyList<ResourceListItemDto>>
{
    public async Task<IReadOnlyList<ResourceListItemDto>> Handle(
        GetFeaturedResourcesQuery request, CancellationToken ct)
    {
        var entities = await db.Resources.AsNoTracking()
            .Where(r => r.IsFeatured && r.Status == ResourceStatus.Published)
            .OrderBy(r => r.FeaturedOrder)
            .Take(6)
            .ToListAsync(ct);

        return entities.Select(ResourceMapping.ToListItem).ToList();
    }
}
