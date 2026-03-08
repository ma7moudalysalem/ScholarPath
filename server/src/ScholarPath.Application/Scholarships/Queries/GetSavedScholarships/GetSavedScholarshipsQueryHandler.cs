using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Scholarships.Queries.GetSavedScholarships;

public class GetSavedScholarshipsQueryHandler
    : IRequestHandler<GetSavedScholarshipsQuery, PaginatedResponse<ScholarshipListItemDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetSavedScholarshipsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedResponse<ScholarshipListItemDto>> Handle(
        GetSavedScholarshipsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 1 : request.PageSize;
        if (pageSize > 100) pageSize = 100;

        var query = _dbContext.SavedScholarships
            .AsNoTracking()
            .Where(ss => ss.UserId == request.UserId)
            .Join(
                _dbContext.Scholarships.AsNoTracking()
                    .Where(s => s.Status == ScholarshipStatus.Published && s.IsActive),
                ss => ss.ScholarshipId,
                s => s.Id,
                (ss, s) => s);

        var totalCount = await query.CountAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ScholarshipListItemDto
            {
                Id = s.Id,
                Title = s.Title,
                TitleAr = s.TitleAr,
                ProviderName = s.ProviderName,
                ProviderNameAr = s.ProviderNameAr,
                Country = s.Country,
                DegreeLevel = s.DegreeLevel,
                FundingType = s.FundingType,
                AwardAmount = s.AwardAmount,
                Currency = s.Currency,
                Deadline = s.Deadline,
                ImageUrl = s.ImageUrl,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            if (item.Deadline.HasValue)
            {
                item.DeadlineCountdownDays = (item.Deadline.Value.Date - today).Days;
                item.IsExpiringSoon = item.DeadlineCountdownDays is > 0 and <= 7;
            }

            item.IsSaved = true;
        }

        return new PaginatedResponse<ScholarshipListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
