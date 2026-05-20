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

        if (!string.IsNullOrEmpty(request.FieldOfStudy))
            query = query.Where(s => s.FieldsOfStudyJson == null ||
                                     s.FieldsOfStudyJson.Contains(request.FieldOfStudy));

        //  Sorting with Tie-break for Pagination Stability
        query = query.OrderByDescending(s => s.IsFeatured)
                     .ThenByDescending(s => s.CreatedAt)
                     .ThenBy(s => s.Id);

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
                OwnerCompanyName = s.OwnerCompany != null ? s.OwnerCompany.FirstName + " " + s.OwnerCompany.LastName : "Global Provider",
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
            OwnerCompanyName = s.OwnerCompanyName,
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
