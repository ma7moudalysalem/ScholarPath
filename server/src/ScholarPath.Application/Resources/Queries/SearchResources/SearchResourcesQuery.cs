using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Resources.Queries.SearchResources;

/// <summary>Public browse/search over published resources (PB-009 AC#4).</summary>
public sealed record SearchResourcesQuery : IRequest<PaginatedList<ResourceListItemDto>>
{
    public string? Term { get; init; }
    public string? CategorySlug { get; init; }
    public string? Tag { get; init; }
    public string? AuthorRole { get; init; }
    public ResourceType? Type { get; init; }
    public string? Language { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
}

public sealed class SearchResourcesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<SearchResourcesQuery, PaginatedList<ResourceListItemDto>>
{
    public async Task<PaginatedList<ResourceListItemDto>> Handle(
        SearchResourcesQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var q = db.Resources.AsNoTracking()
            .Where(r => r.Status == ResourceStatus.Published);

        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            var term = request.Term.Trim();
            var lang = request.Language?.Trim().ToLower();
            q = lang switch
            {
                "ar" => q.Where(r => r.TitleAr.Contains(term)
                    || (r.ContentMarkdownAr != null && r.ContentMarkdownAr.Contains(term))),
                "en" => q.Where(r => r.TitleEn.Contains(term)
                    || (r.ContentMarkdownEn != null && r.ContentMarkdownEn.Contains(term))),
                _ => q.Where(r => r.TitleEn.Contains(term) || r.TitleAr.Contains(term)
                    || (r.ContentMarkdownEn != null && r.ContentMarkdownEn.Contains(term))
                    || (r.ContentMarkdownAr != null && r.ContentMarkdownAr.Contains(term))),
            };
        }

        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
            q = q.Where(r => r.CategorySlug == request.CategorySlug);

        if (!string.IsNullOrWhiteSpace(request.Tag))
            q = q.Where(r => r.TagsJson != null && r.TagsJson.Contains(request.Tag));

        if (!string.IsNullOrWhiteSpace(request.AuthorRole))
            q = q.Where(r => r.AuthorRole == request.AuthorRole);

        if (request.Type.HasValue)
            q = q.Where(r => r.Type == request.Type);

        var total = await q.CountAsync(ct);

        var entities = await q
            .OrderByDescending(r => r.IsFeatured)
            .ThenByDescending(r => r.PublishedAt)
            .ThenBy(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = entities.Select(ResourceMapping.ToListItem).ToList();
        return new PaginatedList<ResourceListItemDto>(items, total, page, pageSize);
    }
}
