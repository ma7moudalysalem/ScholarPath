using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Scholarships.Queries;

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Lists the featured, published (Open) scholarships for the home page /
/// dashboards, ordered by <c>FeaturedOrder</c> then by soonest deadline.
/// Anonymous-friendly — mirrors <see cref="GetScholarshipsQuery"/> projection
/// and <c>Accept-Language</c> localisation, returning flat
/// <see cref="ScholarshipDto"/> rows.
/// </summary>
public record GetFeaturedScholarshipsQuery : IRequest<IReadOnlyList<ScholarshipDto>>
{
    public string Language { get; init; } = "en";

    /// <summary>Upper bound on the number of featured rows returned.</summary>
    public int Limit { get; init; } = 6;
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public class GetFeaturedScholarshipsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetFeaturedScholarshipsQuery, IReadOnlyList<ScholarshipDto>>
{
    public async Task<IReadOnlyList<ScholarshipDto>> Handle(
        GetFeaturedScholarshipsQuery request, CancellationToken ct)
    {
        var lang = request.Language.ToLower() == "ar" ? "ar" : "en";
        var limit = Math.Clamp(request.Limit, 1, 24);

        return await db.Scholarships
            .AsNoTracking()
            .Where(s => s.IsFeatured && s.Status == ScholarshipStatus.Open && !s.IsDeleted)
            .OrderBy(s => s.FeaturedOrder)
            .ThenBy(s => s.Deadline)
            .ThenBy(s => s.Id)
            .Take(limit)
            .Select(s => new ScholarshipDto
            {
                Id = s.Id,
                Title = lang == "ar" ? (s.TitleAr ?? s.TitleEn) : (s.TitleEn ?? s.TitleAr),
                Description = lang == "ar" ? s.DescriptionAr : s.DescriptionEn,
                CategoryName = lang == "ar" ? s.Category!.NameAr : s.Category!.NameEn,
                OwnerCompanyName = s.OwnerCompany != null
                    ? s.OwnerCompany.FirstName + " " + s.OwnerCompany.LastName
                    : "Global Provider",
                Deadline = s.Deadline,
                Status = s.Status.ToString(),
                FundingType = s.FundingType.ToString(),
                TargetLevel = s.TargetLevel.ToString(),
                IsFeatured = s.IsFeatured,
                Slug = s.Slug
            })
            .ToListAsync(ct);
    }
}
