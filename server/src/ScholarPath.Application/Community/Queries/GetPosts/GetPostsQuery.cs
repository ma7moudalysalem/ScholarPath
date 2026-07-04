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
            var term = request.SearchQuery;
            query = query.Where(p =>
                (p.Title != null && p.Title.Contains(term)) ||
                p.BodyMarkdown.Contains(term) ||
                (p.TitleEn != null && p.TitleEn.Contains(term)) ||
                (p.TitleAr != null && p.TitleAr.Contains(term)) ||
                (p.BodyEn != null && p.BodyEn.Contains(term)) ||
                (p.BodyAr != null && p.BodyAr.Contains(term)));
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

        // Personal block: hide posts authored by anyone the current user has
        // blocked (blocker-only — the block never affects other viewers).
        if (currentUserId is Guid blockerId)
        {
            query = query.Where(p => !db.UserBlocks.Any(
                b => b.BlockerId == blockerId && b.BlockedUserId == p.AuthorId));
        }

        // "Trending" = recent + high engagement (last 30 days, scored by net votes
        // plus replies) rather than all-time most-voted, so a genuinely active
        // post surfaces over an old one with a big vote count.
        var trendingSince = DateTimeOffset.UtcNow.AddDays(-30);
        query = request.SortBy switch
        {
            "MostVoted" => query
                .OrderByDescending(p => p.UpvoteCount - p.DownvoteCount)
                .ThenByDescending(p => p.CreatedAt),
            "Trending" => query
                .Where(p => p.CreatedAt >= trendingSince)
                .OrderByDescending(p => p.UpvoteCount - p.DownvoteCount + p.ReplyCount)
                .ThenByDescending(p => p.CreatedAt),
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
                p.TitleEn,
                p.TitleAr,
                p.BodyEn,
                p.BodyAr,
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
                r.Tags, r.IsBookmarked,
                r.TitleEn, r.TitleAr, r.BodyEn ?? r.BodyMarkdown, r.BodyAr))
            .ToList();

        return new PagedResult<ForumPostDto>(items, request.Page, request.PageSize, total);
    }
}
