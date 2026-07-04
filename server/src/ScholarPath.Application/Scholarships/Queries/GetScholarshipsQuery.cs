using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Scholarships.Queries;

public record GetScholarshipsQuery : IRequest<PaginatedList<ScholarshipDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Term { get; init; }
    public string Language { get; init; } = "en";
    public Guid? CategoryId { get; init; }
    public FundingType? FundingType { get; init; }
    public AcademicLevel? AcademicLevel { get; init; }
    public string? Country { get; init; }
    public DateTimeOffset? DeadlineFrom { get; init; }
    public DateTimeOffset? DeadlineTo { get; init; }
    public string[]? Tags { get; init; }
    public string? FieldOfStudy { get; init; }
    public bool? FundedOnly { get; init; }

    /// <summary>
    /// Status filter — defaults to <see cref="ScholarshipStatus.Open"/> so
    /// students never see Draft, UnderReview, Closed, or Archived listings
    /// mixed in. Admins may pass an explicit value to inspect other states.
    /// </summary>
    public ScholarshipStatus? Status { get; init; } = ScholarshipStatus.Open;

    /// <summary>
    /// Optional sort order. <c>deadline</c> sorts ascending by Deadline,
    /// <c>newest</c> sorts descending by CreatedAt. When omitted, the default
    /// ordering is featured-first then newest.
    /// </summary>
    public string? Sort { get; init; }
}

public class GetScholarshipsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetScholarshipsQuery, PaginatedList<ScholarshipDto>>
{
    public async Task<PaginatedList<ScholarshipDto>> Handle(GetScholarshipsQuery request, CancellationToken ct)
    {
        var lang = request.Language.ToLower() == "ar" ? "ar" : "en";
        var userId = currentUser.UserId;
        var query = db.Scholarships.AsNoTracking();

        // Status — defaults to Open so students never see Draft / UnderReview
        // / Closed / Archived rows mixed in (FR-SCH-02). The class-level
        // [Authorize] on the controller plus role-based UI gating means an
        // admin can opt out by passing an explicit value.
        if (request.Status.HasValue)
            query = query.Where(s => s.Status == request.Status.Value);

        //  Search (LIKE — no full-text index required)
        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            // Previously used SQL Server CONTAINS, which needs a full-text catalog
            // on Scholarships that is not provisioned in this DB — so every search
            // threw and surfaced to users as "search failed". LIKE works without an
            // index. Escape LIKE wildcards so user input can't act as a pattern.
            var sanitized = request.Term.Trim();
            if (!string.IsNullOrWhiteSpace(sanitized))
            {
                var escaped = sanitized
                    .Replace("[", "[[]")
                    .Replace("%", "[%]")
                    .Replace("_", "[_]");
                var like = $"%{escaped}%";
                query = query.Where(s =>
                    EF.Functions.Like(s.TitleEn, like) ||
                    EF.Functions.Like(s.TitleAr, like) ||
                    EF.Functions.Like(s.DescriptionEn, like) ||
                    EF.Functions.Like(s.DescriptionAr, like) ||
                    // FR-SCH-04: also match on the provider's name.
                    (s.OwnerScholarshipProvider != null &&
                     EF.Functions.Like(
                         s.OwnerScholarshipProvider.FirstName + " " + s.OwnerScholarshipProvider.LastName,
                         like)));
            }
        }

        //  Filters
        if (request.CategoryId.HasValue) query = query.Where(s => s.CategoryId == request.CategoryId);
        if (request.FundingType.HasValue) query = query.Where(s => s.FundingType == request.FundingType);
        if (request.AcademicLevel.HasValue) query = query.Where(s => s.TargetLevel == request.AcademicLevel);
        if (request.FundedOnly == true) query = query.Where(s => s.FundingType == FundingType.FullyFunded ||
                             s.FundingType == FundingType.PartiallyFunded);

        if (request.DeadlineFrom.HasValue) query = query.Where(s => s.Deadline >= request.DeadlineFrom);
        if (request.DeadlineTo.HasValue) query = query.Where(s => s.Deadline <= request.DeadlineTo);

        if (!string.IsNullOrEmpty(request.Country))
            query = query.Where(s => s.TargetCountriesJson!.Contains(request.Country));

        if (!string.IsNullOrEmpty(request.FieldOfStudy))
        {
            // FR-SCH-05: match the field as a whole JSON-array element, not a raw
            // substring — otherwise "Art" wrongly matched "Smart Materials".
            // Serialize the needle with the SAME serializer used to store the
            // array (CreateScholarshipCommand). That way the surrounding quotes
            // AND the default encoder's escaping of characters such as ampersand
            // both line up with the stored value, so fields like "Arts and
            // Humanities" written with an ampersand still match. A listing with
            // no fields declared is unrestricted (matches every field filter).
            var fieldNeedle = JsonSerializer.Serialize(request.FieldOfStudy);
            query = query.Where(s => s.FieldsOfStudyJson == null ||
                                     s.FieldsOfStudyJson.Contains(fieldNeedle));
        }

        //  Sorting with Tie-break for Pagination Stability
        // `Sort` is an opt-in override. The default keeps featured-first then
        // newest, mirroring the SRS-default ordering.
        // OrdinalIgnoreCase keeps the analyzer happy (CA1308) — we're matching
        // a small, fixed set of literals, not normalising for de-dup.
        if (string.Equals(request.Sort, "deadline", StringComparison.OrdinalIgnoreCase))
        {
            query = query.OrderBy(s => s.Deadline).ThenBy(s => s.Id);
        }
        else if (string.Equals(request.Sort, "newest", StringComparison.OrdinalIgnoreCase))
        {
            query = query.OrderByDescending(s => s.CreatedAt).ThenBy(s => s.Id);
        }
        else
        {
            query = query.OrderByDescending(s => s.IsFeatured)
                         .ThenByDescending(s => s.CreatedAt)
                         .ThenBy(s => s.Id);
        }

        //  Count for pagination
        var totalCount = await query.CountAsync(ct);

        //  Projection with Language Fallback (raw JSON fetched; deserialized after materialisation)
        var raw = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new
            {
                s.Id,
                Title = lang == "ar" ? (s.TitleAr ?? s.TitleEn) : (s.TitleEn ?? s.TitleAr),
                Description = lang == "ar" ? s.DescriptionAr : s.DescriptionEn,
                CategoryName = lang == "ar" ? s.Category!.NameAr : s.Category!.NameEn,
                OwnerScholarshipProviderName = s.OwnerScholarshipProvider != null ? s.OwnerScholarshipProvider.FirstName + " " + s.OwnerScholarshipProvider.LastName : "Global Provider",
                s.Deadline,
                Status = s.Status.ToString(),
                FundingType = s.FundingType.ToString(),
                TargetLevel = s.TargetLevel.ToString(),
                s.IsFeatured,
                s.Slug,
                s.FieldsOfStudyJson,
                IsBookmarked = userId != null && db.SavedScholarships.Any(
                    sv => sv.ScholarshipId == s.Id && sv.UserId == userId),
            })
            .ToListAsync(ct);

        var items = raw.Select(s => new ScholarshipDto
        {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            CategoryName = s.CategoryName,
            OwnerScholarshipProviderName = s.OwnerScholarshipProviderName,
            Deadline = s.Deadline,
            Status = s.Status,
            FundingType = s.FundingType,
            TargetLevel = s.TargetLevel,
            IsFeatured = s.IsFeatured,
            Slug = s.Slug,
            FieldsOfStudy = s.FieldsOfStudyJson is not null
                ? JsonSerializer.Deserialize<List<string>>(s.FieldsOfStudyJson) ?? []
                : [],
            IsBookmarked = s.IsBookmarked,
        }).ToList();

        return new PaginatedList<ScholarshipDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
