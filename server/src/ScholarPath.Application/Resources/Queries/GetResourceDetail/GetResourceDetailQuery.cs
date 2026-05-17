using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Resources.Queries.GetResourceDetail;

/// <summary>
/// Full resource detail by id or slug (PB-009). An unpublished resource is visible
/// only to its author or an admin.
/// </summary>
public sealed record GetResourceDetailQuery(string IdOrSlug) : IRequest<ResourceDetailDto?>;

public sealed class GetResourceDetailQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetResourceDetailQuery, ResourceDetailDto?>
{
    public async Task<ResourceDetailDto?> Handle(GetResourceDetailQuery request, CancellationToken ct)
    {
        var q = db.Resources.AsNoTracking()
            .Include(r => r.Chapters)
            .Include(r => r.Author);

        var resource = Guid.TryParse(request.IdOrSlug, out var id)
            ? await q.FirstOrDefaultAsync(r => r.Id == id, ct)
            : await q.FirstOrDefaultAsync(r => r.Slug == request.IdOrSlug, ct);

        if (resource is null) return null;

        if (resource.Status != ResourceStatus.Published)
        {
            var canSeeDraft = currentUser.IsInRole("Admin")
                || (currentUser.UserId is { } uid && uid == resource.AuthorUserId);
            if (!canSeeDraft) return null;
        }

        var chapters = resource.Chapters
            .OrderBy(c => c.SortOrder)
            .Select(c => new ResourceChapterDto(
                c.Id, c.TitleEn, c.TitleAr,
                c.ContentMarkdownEn, c.ContentMarkdownAr,
                c.SortOrder, c.EstimatedReadMinutes))
            .ToList();

        return new ResourceDetailDto(
            resource.Id, resource.Slug, resource.TitleEn, resource.TitleAr,
            resource.DescriptionEn, resource.DescriptionAr,
            resource.ContentMarkdownEn, resource.ContentMarkdownAr,
            resource.ExternalLinkUrl, resource.CoverImageUrl,
            resource.AuthorUserId, resource.AuthorRole, resource.Author?.FullName,
            resource.Type, resource.Status, resource.CategorySlug,
            ResourceTags.Deserialize(resource.TagsJson),
            resource.IsFeatured, resource.PublishedAt, resource.RejectionReason,
            chapters);
    }
}
