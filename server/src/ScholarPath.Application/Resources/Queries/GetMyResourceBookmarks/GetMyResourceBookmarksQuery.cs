using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Resources.Queries.GetMyResourceBookmarks;

/// <summary>The authenticated student's bookmarked resources, newest first (PB-009 AC#5).</summary>
public sealed record GetMyResourceBookmarksQuery : IRequest<IReadOnlyList<ResourceBookmarkDto>>;

public sealed class GetMyResourceBookmarksQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyResourceBookmarksQuery, IReadOnlyList<ResourceBookmarkDto>>
{
    public async Task<IReadOnlyList<ResourceBookmarkDto>> Handle(
        GetMyResourceBookmarksQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        return await (
            from b in db.ResourceBookmarks.AsNoTracking()
            where b.UserId == userId
            join r in db.Resources.AsNoTracking() on b.ResourceId equals r.Id
            orderby b.BookmarkedAt descending
            select new ResourceBookmarkDto(
                r.Id, r.Slug, r.TitleEn, r.TitleAr, r.Type, r.CoverImageUrl, b.BookmarkedAt))
            .ToListAsync(ct);
    }
}
