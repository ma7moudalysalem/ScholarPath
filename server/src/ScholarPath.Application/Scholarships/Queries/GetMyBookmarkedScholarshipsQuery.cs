using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Scholarships.DTOs;

namespace ScholarPath.Application.Scholarships.Queries;

// ─── DTO ──────────────────────────────────────────────────────────────────────

/// <summary>
/// A single saved-scholarship row for the student's bookmarks list — the
/// localised <see cref="ScholarshipDto"/> plus the bookmark metadata
/// (<c>SavedAt</c> / optional <c>Note</c>).
/// </summary>
public record BookmarkedScholarshipDto
{
    /// <summary>Identifier of the <c>SavedScholarship</c> bookmark row.</summary>
    public Guid Id { get; init; }
    public Guid ScholarshipId { get; init; }
    public DateTimeOffset SavedAt { get; init; }
    public string? Note { get; init; }
    public ScholarshipDto Scholarship { get; init; } = default!;
}

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Lists the scholarships the authenticated user has bookmarked, newest-saved
/// first. Mirrors <see cref="GetScholarshipsQuery"/> / GetMyScholarshipsQuery
/// (AsNoTracking projection, <c>Accept-Language</c> localisation, flat DTO).
/// </summary>
public record GetMyBookmarkedScholarshipsQuery : IRequest<IReadOnlyList<BookmarkedScholarshipDto>>
{
    public string Language { get; init; } = "en";
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public class GetMyBookmarkedScholarshipsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyBookmarkedScholarshipsQuery, IReadOnlyList<BookmarkedScholarshipDto>>
{
    public async Task<IReadOnlyList<BookmarkedScholarshipDto>> Handle(
        GetMyBookmarkedScholarshipsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var lang = request.Language.ToLower() == "ar" ? "ar" : "en";

        // Inner-join bookmark → scholarship: a bookmark whose scholarship was
        // soft-deleted simply drops out of the list (no orphan rows, no throw).
        var query =
            from bookmark in db.SavedScholarships.AsNoTracking()
            where bookmark.UserId == userId
            join scholarship in db.Scholarships.AsNoTracking().Where(s => !s.IsDeleted)
                on bookmark.ScholarshipId equals scholarship.Id
            orderby bookmark.SavedAt descending, bookmark.Id descending
            select new BookmarkedScholarshipDto
            {
                Id = bookmark.Id,
                ScholarshipId = bookmark.ScholarshipId,
                SavedAt = bookmark.SavedAt,
                Note = bookmark.Note,
                Scholarship = new ScholarshipDto
                {
                    Id = scholarship.Id,
                    Title = lang == "ar"
                        ? (scholarship.TitleAr ?? scholarship.TitleEn)
                        : (scholarship.TitleEn ?? scholarship.TitleAr),
                    Description = lang == "ar" ? scholarship.DescriptionAr : scholarship.DescriptionEn,
                    CategoryName = lang == "ar"
                        ? scholarship.Category!.NameAr
                        : scholarship.Category!.NameEn,
                    OwnerCompanyName = scholarship.OwnerCompany != null
                        ? scholarship.OwnerCompany.FirstName + " " + scholarship.OwnerCompany.LastName
                        : "Global Provider",
                    Deadline = scholarship.Deadline,
                    Status = scholarship.Status.ToString(),
                    FundingType = scholarship.FundingType.ToString(),
                    TargetLevel = scholarship.TargetLevel.ToString(),
                    IsFeatured = scholarship.IsFeatured,
                    Slug = scholarship.Slug,
                    // Every row in this list is, by definition, bookmarked.
                    IsBookmarked = true
                }
            };

        return await query.ToListAsync(ct);
    }
}
