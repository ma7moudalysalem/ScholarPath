using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Resources.Queries.GetMyResourceProgress;

/// <summary>The authenticated student's per-resource reading progress (PB-009 AC#6).</summary>
public sealed record GetMyResourceProgressQuery : IRequest<IReadOnlyList<ResourceProgressDto>>;

public sealed class GetMyResourceProgressQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyResourceProgressQuery, IReadOnlyList<ResourceProgressDto>>
{
    public async Task<IReadOnlyList<ResourceProgressDto>> Handle(
        GetMyResourceProgressQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        return await (
            from p in db.ResourceProgress.AsNoTracking()
            where p.UserId == userId
            join r in db.Resources.AsNoTracking() on p.ResourceId equals r.Id
            orderby p.LastAccessedAt descending
            select new ResourceProgressDto(
                r.Id, r.Slug, r.TitleEn, r.TitleAr,
                p.ChaptersCompletedCount,
                db.ResourceChapters.Count(c => c.ResourceId == r.Id),
                p.LastAccessedAt))
            .ToListAsync(ct);
    }
}
