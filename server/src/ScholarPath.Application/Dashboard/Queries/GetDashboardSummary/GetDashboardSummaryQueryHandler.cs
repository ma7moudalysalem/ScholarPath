using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Dashboard.DTOs;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICachingService _cachingService;

    public GetDashboardSummaryQueryHandler(
        IApplicationDbContext dbContext,
        ICachingService cachingService)
    {
        _dbContext = dbContext;
        _cachingService = cachingService;
    }

    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"dashboard:summary:{request.UserId}";
        var cached = await _cachingService.GetAsync<DashboardSummaryDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        // 1. Count saved scholarships
        var savedCount = await _dbContext.SavedScholarships
            .CountAsync(s => s.UserId == request.UserId, cancellationToken);

        // 2. Count tracked applications by status
        var statusCounts = await _dbContext.ApplicationTrackers
            .Where(a => a.UserId == request.UserId)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // 3. Build StatusCounts dictionary with all keys
        var statusDict = new Dictionary<string, int>
        {
            ["Saved"] = savedCount,
            ["Planned"] = 0,
            ["Applied"] = 0,
            ["Pending"] = 0,
            ["Accepted"] = 0,
            ["Rejected"] = 0
        };

        foreach (var sc in statusCounts)
        {
            statusDict[sc.Status.ToString()] = sc.Count;
        }

        // 4. Get deadlines within 14 days
        var today = DateTime.UtcNow.Date;
        var in14Days = today.AddDays(14);

        var deadlinesSoon = await _dbContext.ApplicationTrackers
            .Include(a => a.Scholarship)
            .Where(a => a.UserId == request.UserId
                && a.Scholarship.Deadline != null
                && a.Scholarship.Deadline >= today
                && a.Scholarship.Deadline <= in14Days)
            .OrderBy(a => a.Scholarship.Deadline)
            .Select(a => new UpcomingDeadlineDto
            {
                ScholarshipId = a.ScholarshipId,
                Title = a.Scholarship.Title,
                TitleAr = a.Scholarship.TitleAr,
                ProviderName = a.Scholarship.ProviderName,
                Deadline = a.Scholarship.Deadline!.Value,
                CountdownDays = (a.Scholarship.Deadline!.Value - today).Days,
                Status = a.Status
            })
            .ToListAsync(cancellationToken);

        // 5. Build recommended actions
        var recommendedActions = new List<string>();

        var profile = await _dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

        if (profile is null
            || string.IsNullOrWhiteSpace(profile.FieldOfStudy)
            || string.IsNullOrWhiteSpace(profile.Country))
        {
            recommendedActions.Add("Complete your profile for better recommendations");
        }

        var trackedCount = statusCounts.Sum(s => s.Count);
        if (trackedCount == 0)
        {
            recommendedActions.Add("Start tracking scholarships you're interested in");
        }

        if (savedCount == 0)
        {
            recommendedActions.Add("Browse and save scholarships that interest you");
        }

        if (trackedCount > 0)
        {
            var hasReminders = await _dbContext.ApplicationTrackers
                .AnyAsync(a => a.UserId == request.UserId && a.RemindersJson != null, cancellationToken);

            if (!hasReminders)
            {
                recommendedActions.Add("Set deadline reminders for your tracked scholarships");
            }
        }

        var result = new DashboardSummaryDto
        {
            StatusCounts = statusDict,
            DeadlinesSoon = deadlinesSoon,
            RecommendedActions = recommendedActions
        };

        // 6. Cache with 2-minute TTL
        await _cachingService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2), cancellationToken);

        return result;
    }
}
