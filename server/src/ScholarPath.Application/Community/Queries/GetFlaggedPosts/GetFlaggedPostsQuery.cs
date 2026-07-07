using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Community.Queries.GetFlaggedPosts;

// ─── DTO ──────────────────────────────────────────────────────────────────────

/// <summary>An individual report on a flagged post — shown to the admin so they can
/// judge the reason(s) before deciding (PB-007). Reporter identity is intentionally
/// omitted for privacy; the reason + details are what the decision needs.</summary>
public sealed record FlagDetailDto(
    string Reason,
    string? AdditionalDetails,
    DateTimeOffset FlaggedAt);

/// <summary>A post that needs admin attention — flagged by users or auto-hidden (PB-007).</summary>
public sealed record FlaggedPostDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string? Title,
    string BodyPreview,
    // Full post body so the admin can expand and read the WHOLE post before
    // deciding — the 240-char preview can hide the flagged content (a bad link
    // or slur past the cut). The list row still shows BodyPreview collapsed.
    string Body,
    int FlagCount,
    int ValidFlagCount,
    string? TopFlagReason,
    PostModerationStatus ModerationStatus,
    bool IsAutoHidden,
    DateTimeOffset? AutoHiddenAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<FlagDetailDto> Flags);

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Lists posts in the moderation queue — those carrying at least one valid flag or
/// already auto-hidden — most-flagged first. Admin-only, paged.
/// </summary>
public sealed record GetFlaggedPostsQuery(int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<FlaggedPostDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetFlaggedPostsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetFlaggedPostsQuery, PagedResult<FlaggedPostDto>>
{
    private const int PreviewLength = 240;

    public async Task<PagedResult<FlaggedPostDto>> Handle(GetFlaggedPostsQuery request, CancellationToken ct)
    {
        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("Only an administrator can moderate the community.");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.ForumPosts
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Flags)
            .Where(p => !p.IsDeleted
                && (p.IsAutoHidden || p.Flags.Any(f => f.IsValid)));

        var total = await query.CountAsync(ct).ConfigureAwait(false);

        var posts = await query
            .OrderByDescending(p => p.IsAutoHidden)
            .ThenByDescending(p => p.FlagCount)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var items = posts
            .Select(p => new FlaggedPostDto(
                p.Id,
                p.AuthorId,
                p.Author?.FullName ?? "Anonymous",
                p.Title,
                Preview(p.BodyMarkdown),
                p.BodyMarkdown ?? string.Empty,
                p.FlagCount,
                p.Flags.Count(f => f.IsValid),
                p.Flags.Where(f => f.IsValid)
                    .OrderByDescending(f => f.FlaggedAt)
                    .Select(f => f.Reason)
                    .FirstOrDefault(),
                p.ModerationStatus,
                p.IsAutoHidden,
                p.AutoHiddenAt,
                p.CreatedAt,
                p.Flags.Where(f => f.IsValid)
                    .OrderByDescending(f => f.FlaggedAt)
                    .Select(f => new FlagDetailDto(f.Reason, f.AdditionalDetails, f.FlaggedAt))
                    .ToList()))
            .ToList();

        return new PagedResult<FlaggedPostDto>(items, page, pageSize, total);
    }

    private static string Preview(string body)
    {
        if (string.IsNullOrEmpty(body)) return string.Empty;
        var trimmed = body.Trim();
        return trimmed.Length <= PreviewLength
            ? trimmed
            : trimmed[..PreviewLength] + "…";
    }
}
