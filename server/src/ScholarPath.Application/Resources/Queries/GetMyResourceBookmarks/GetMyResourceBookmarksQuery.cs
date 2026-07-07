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

        // Materialize the raw rows first, then map — tags are stored as a JSON
        // string on the entity and ResourceTags.Deserialize cannot be translated
        // to SQL, so it must run in memory over the fetched rows.
        var rows = await (
            from b in db.ResourceBookmarks.AsNoTracking()
            where b.UserId == userId
            join r in db.Resources.AsNoTracking() on b.ResourceId equals r.Id
            orderby b.BookmarkedAt descending
            select new
            {
                r.Id,
                r.Slug,
                r.TitleEn,
                r.TitleAr,
                r.DescriptionEn,
                r.DescriptionAr,
                r.Type,
                r.CoverImageUrl,
                r.TagsJson,
                b.BookmarkedAt,
            })
            .ToListAsync(ct);

        return rows
            .Select(x => new ResourceBookmarkDto(
                x.Id, x.Slug, x.TitleEn, x.TitleAr,
                x.DescriptionEn, x.DescriptionAr,
                x.Type, x.CoverImageUrl,
                ResourceTags.Deserialize(x.TagsJson),
                x.BookmarkedAt))
            .ToList();
    }
}
