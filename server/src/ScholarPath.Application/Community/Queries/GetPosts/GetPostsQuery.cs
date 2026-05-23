using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Community.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Community.Queries.GetPosts;

public sealed record GetPostsQuery(
    Guid? CategoryId = null,
    string? SearchQuery = null,
    string SortBy = "Newest", // "Newest", "MostVoted"
    string? Tag = null,
    bool BookmarkedOnly = false,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ForumPostDto>>;

public sealed class GetPostsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetPostsQuery, PagedResult<ForumPostDto>>
{
    public async Task<PagedResult<ForumPostDto>> Handle(GetPostsQuery request, CancellationToken ct)
    {
        var currentUserId = currentUser.UserId;

        var query = db.ForumPosts
            .AsNoTracking()
            .Include(p => p.Author)
            .Where(p => p.ParentPostId == null && !p.IsDeleted && !p.IsAutoHidden);

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            query = query.Where(p => p.Title!.Contains(request.SearchQuery) || p.BodyMarkdown.Contains(request.SearchQuery));
        }

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            // Lowercase by design — slugs are stored lowercase. CA1308 wants
            // uppercase normalisation but that would break the lookup.
#pragma warning disable CA1308
            var tagSlug = request.Tag.Trim().ToLowerInvariant();
#pragma warning restore CA1308
            query = query.Where(p => p.PostTags.Any(pt => pt.ForumTag!.Slug == tagSlug));
        }

        if (request.BookmarkedOnly && currentUserId is Guid uid)
        {
            query = query.Where(p => p.Bookmarks.Any(b => b.UserId == uid));
        }
        else if (request.BookmarkedOnly && currentUserId is null)
        {
            // Anonymous user asking for bookmarks — return an empty page.
            return new PagedResult<ForumPostDto>(Array.Empty<ForumPostDto>(), request.Page, request.PageSize, 0);
        }

        query = request.SortBy switch
        {
            "MostVoted" => query.OrderByDescending(p => p.UpvoteCount - p.DownvoteCount),
            _ => query.OrderByDescending(p => p.CreatedAt),
        };

        var total = await query.CountAsync(ct).ConfigureAwait(false);

        var rows = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new
            {
                p.Id,
                p.AuthorId,
                AuthorName = p.Author!.FullName ?? "Anonymous",
                p.CategoryId,
                p.Title,
                p.BodyMarkdown,
                p.UpvoteCount,
                p.DownvoteCount,
                p.ReplyCount,
                p.CreatedAt,
                Tags = p.PostTags.Select(pt => pt.ForumTag!.Slug).OrderBy(s => s).ToList(),
                IsBookmarked = currentUserId != null && p.Bookmarks.Any(b => b.UserId == currentUserId),
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var items = rows
            .Select(r => new ForumPostDto(
                r.Id, r.AuthorId, r.AuthorName, r.CategoryId, r.Title, r.BodyMarkdown,
                r.UpvoteCount, r.DownvoteCount, r.ReplyCount, r.CreatedAt,
                r.Tags, r.IsBookmarked))
            .ToList();

        return new PagedResult<ForumPostDto>(items, request.Page, request.PageSize, total);
    }
}
