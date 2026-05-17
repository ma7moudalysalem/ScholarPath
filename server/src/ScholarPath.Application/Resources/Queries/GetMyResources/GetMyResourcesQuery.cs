using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Resources.Queries.GetMyResources;

/// <summary>The authenticated author's own resources, any status (PB-009).</summary>
public sealed record GetMyResourcesQuery : IRequest<IReadOnlyList<ResourceListItemDto>>;

public sealed class GetMyResourcesQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyResourcesQuery, IReadOnlyList<ResourceListItemDto>>
{
    public async Task<IReadOnlyList<ResourceListItemDto>> Handle(
        GetMyResourcesQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var entities = await db.Resources.AsNoTracking()
            .Where(r => r.AuthorUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(ResourceMapping.ToListItem).ToList();
    }
}
