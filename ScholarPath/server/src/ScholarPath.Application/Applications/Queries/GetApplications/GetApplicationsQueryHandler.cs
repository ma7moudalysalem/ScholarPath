using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Applications.Queries.GetApplications;

public class GetApplicationsQueryHandler
    : IRequestHandler<GetApplicationsQuery, PaginatedResponse<ApplicationListItemDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetApplicationsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedResponse<ApplicationListItemDto>> Handle(
        GetApplicationsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.ApplicationTrackers
            .AsNoTracking()
            .Include(a => a.Scholarship)
            .Where(a => a.UserId == request.UserId);

        // Filter by status
        if (request.Status.HasValue)
            query = query.Where(a => a.Status == request.Status.Value);

        // Sorting
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "deadline" => query.OrderBy(a => a.Scholarship.Deadline),
            _ => query.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new ApplicationListItemDto
            {
                Id = a.Id,
                ScholarshipId = a.ScholarshipId,
                ScholarshipTitle = a.Scholarship.Title,
                ScholarshipTitleAr = a.Scholarship.TitleAr,
                ProviderName = a.Scholarship.ProviderName,
                Deadline = a.Scholarship.Deadline,
                Status = a.Status,
                NotesPreview = a.Notes != null
                    ? (a.Notes.Length > 100 ? a.Notes.Substring(0, 100) : a.Notes)
                    : null,
                HasReminders = a.Reminders.Any(),
                UpdatedAt = a.UpdatedAt,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Compute derived fields in memory
        foreach (var item in items)
        {
            if (item.Deadline.HasValue)
            {
                item.DeadlineCountdownDays = (item.Deadline.Value.Date - today).Days;
                item.IsOverdue = item.DeadlineCountdownDays < 0;
            }
        }

        return new PaginatedResponse<ApplicationListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
