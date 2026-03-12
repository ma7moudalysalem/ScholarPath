using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Scholarships.Queries.GetScholarshipDetail;

public class GetScholarshipDetailQueryHandler
    : IRequestHandler<GetScholarshipDetailQuery, Result<ScholarshipDetailDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICachingService _cachingService;

    public GetScholarshipDetailQueryHandler(
        IApplicationDbContext dbContext,
        ICachingService cachingService)
    {
        _dbContext = dbContext;
        _cachingService = cachingService;
    }

    public async Task<Result<ScholarshipDetailDto>> Handle(
        GetScholarshipDetailQuery request, CancellationToken cancellationToken)
    {
        // Cache scholarship detail (shared cache — user-specific fields like IsSaved/IsTracked are resolved after)
        var cacheKey = $"scholarship:detail:{request.ScholarshipId}";
        var cachedBase = await _cachingService.GetAsync<ScholarshipDetailDto>(cacheKey, cancellationToken);

        ScholarshipDetailDto dto;

        if (cachedBase is not null)
        {
            dto = cachedBase;
        }
        else
        {
            var scholarship = await _dbContext.Scholarships
                .AsNoTracking()
                .Include(s => s.Category)
                .Include(s => s.Tags)
                .Include(s => s.DocumentsChecklist)
                .Include(s => s.EligibleCountries)
                .Include(s => s.EligibleMajors)
                .FirstOrDefaultAsync(s => s.Id == request.ScholarshipId, cancellationToken);

            if (scholarship is null || scholarship.Status != ScholarshipStatus.Published)
                return Result<ScholarshipDetailDto>.Failure("Scholarship not found.");

            var today = DateTime.UtcNow.Date;

            dto = new ScholarshipDetailDto
            {
                Id = scholarship.Id,
                Title = scholarship.Title,
                TitleAr = scholarship.TitleAr,
                Description = scholarship.Description,
                DescriptionAr = scholarship.DescriptionAr,
                ProviderName = scholarship.ProviderName,
                ProviderNameAr = scholarship.ProviderNameAr,
                Country = scholarship.Country,
                FieldOfStudy = scholarship.FieldOfStudy,
                DegreeLevel = scholarship.DegreeLevel,
                FundingType = scholarship.FundingType,
                AwardAmount = scholarship.AwardAmount,
                Currency = scholarship.Currency,
                Deadline = scholarship.Deadline,
                DeadlineCountdownDays = scholarship.Deadline.HasValue
                    ? (scholarship.Deadline.Value.Date - today).Days
                    : null,
                EligibilityDescription = scholarship.EligibilityDescription,
                RequiredDocuments = scholarship.RequiredDocuments,
                OverviewHtml = scholarship.OverviewHtml,
                HowToApplyHtml = scholarship.HowToApplyHtml,
                DocumentsChecklist = scholarship.DocumentsChecklist.Select(d => d.DocumentName).ToArray(),
                OfficialLink = scholarship.OfficialLink,
                ImageUrl = scholarship.ImageUrl,
                MinGPA = scholarship.MinGPA,
                MaxAge = scholarship.MaxAge,
                EligibleCountries = scholarship.EligibleCountries.Select(c => c.CountryCode).ToArray(),
                EligibleMajors = scholarship.EligibleMajors.Select(m => m.MajorName).ToArray(),
                Tags = scholarship.Tags.Select(t => t.Name).ToArray(),
                ViewCount = scholarship.ViewCount,
                CategoryId = scholarship.CategoryId,
                CategoryName = scholarship.Category?.Name,
                CreatedAt = scholarship.CreatedAt
            };

            // Cache the shared detail for 5 minutes
            await _cachingService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), cancellationToken);
        }

        // Resolve user-specific fields (not cached — always fresh)
        if (request.CurrentUserId.HasValue)
        {
            dto.IsSaved = await _dbContext.SavedScholarships
                .AsNoTracking()
                .AnyAsync(ss => ss.ScholarshipId == request.ScholarshipId
                    && ss.UserId == request.CurrentUserId.Value, cancellationToken);

            dto.IsTracked = await _dbContext.ApplicationTrackers
                .AsNoTracking()
                .AnyAsync(at => at.ScholarshipId == request.ScholarshipId
                    && at.UserId == request.CurrentUserId.Value, cancellationToken);
        }

        // Increment ViewCount fire-and-forget
        _ = Task.Run(async () =>
        {
            try
            {
                await _dbContext.Scholarships
                    .Where(s => s.Id == request.ScholarshipId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1));
            }
            catch
            {
                // Silently ignore view count increment failures
            }
        }, CancellationToken.None);

        return Result<ScholarshipDetailDto>.Success(dto);
    }
}
