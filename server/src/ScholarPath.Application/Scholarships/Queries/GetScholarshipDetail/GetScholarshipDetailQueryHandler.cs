using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Scholarships.Queries.GetScholarshipDetail;

public class GetScholarshipDetailQueryHandler
    : IRequestHandler<GetScholarshipDetailQuery, Result<ScholarshipDetailDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetScholarshipDetailQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ScholarshipDetailDto>> Handle(
        GetScholarshipDetailQuery request, CancellationToken cancellationToken)
    {
        var scholarship = await _dbContext.Scholarships
            .AsNoTracking()
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == request.ScholarshipId, cancellationToken);

        if (scholarship is null || scholarship.Status != ScholarshipStatus.Published)
            return Result<ScholarshipDetailDto>.Failure("Scholarship not found.");

        var today = DateTime.UtcNow.Date;

        var dto = new ScholarshipDetailDto
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
            DocumentsChecklist = scholarship.DocumentsChecklist,
            OfficialLink = scholarship.OfficialLink,
            ImageUrl = scholarship.ImageUrl,
            MinGPA = scholarship.MinGPA,
            MaxAge = scholarship.MaxAge,
            EligibleCountries = scholarship.EligibleCountries,
            EligibleMajors = scholarship.EligibleMajors,
            Tags = scholarship.Tags,
            ViewCount = scholarship.ViewCount,
            CategoryId = scholarship.CategoryId,
            CategoryName = scholarship.Category?.Name,
            CreatedAt = scholarship.CreatedAt
        };

        // Check if the scholarship is saved by the current user
        if (request.CurrentUserId.HasValue)
        {
            dto.IsSaved = await _dbContext.SavedScholarships
                .AsNoTracking()
                .AnyAsync(ss => ss.ScholarshipId == request.ScholarshipId
                    && ss.UserId == request.CurrentUserId.Value, cancellationToken);
        }

        // Check if the scholarship is tracked by the current user
        if (request.CurrentUserId.HasValue)
        {
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
