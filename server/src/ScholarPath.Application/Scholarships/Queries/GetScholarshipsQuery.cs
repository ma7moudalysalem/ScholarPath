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
    public bool? FundedOnly { get; init; }
}

public class GetScholarshipsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetScholarshipsQuery, PaginatedList<ScholarshipDto>>
{
    public async Task<PaginatedList<ScholarshipDto>> Handle(GetScholarshipsQuery request, CancellationToken ct)
    {
        var lang = request.Language.ToLower() == "ar" ? "ar" : "en";
        var userId = currentUser.UserId;
        var query = db.Scholarships.AsNoTracking();

        //  Full-Text Search 
        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            query = query.Where(s => EF.Functions.Contains(s.TitleEn, request.Term) ||
                                     EF.Functions.Contains(s.TitleAr, request.Term));
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

        //  Sorting with Tie-break for Pagination Stability 
        query = query.OrderByDescending(s => s.IsFeatured)
                     .ThenByDescending(s => s.CreatedAt)
                     .ThenBy(s => s.Id);

        //  Projection with Language Fallback 
        var result = query.Select(s => new ScholarshipDto
        {
            Id = s.Id,
            Title = lang == "ar" ? (s.TitleAr ?? s.TitleEn) : (s.TitleEn ?? s.TitleAr),
            Description = lang == "ar" ? s.DescriptionAr : s.DescriptionEn,
            CategoryName = lang == "ar" ? s.Category!.NameAr : s.Category!.NameEn,
            OwnerCompanyName = s.OwnerCompany != null ? s.OwnerCompany.FirstName + " " + s.OwnerCompany.LastName : "Global Provider",
            Deadline = s.Deadline,
            Status = s.Status.ToString(),
            FundingType = s.FundingType.ToString(),
            TargetLevel = s.TargetLevel.ToString(),
            IsFeatured = s.IsFeatured,
            Slug = s.Slug,
            IsBookmarked = userId != null && db.SavedScholarships.Any(
                sv => sv.ScholarshipId == s.Id && sv.UserId == userId)
        });

        return await PaginatedList<ScholarshipDto>.CreateAsync(result, request.PageNumber, request.PageSize);
    }
}
