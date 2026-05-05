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
        // We intentionally don't cache this dashboard summary per-user, because
        // it contains counts (like Saved) that change frequently. A 2-minute cache
        // causes the UI to appear out of sync immediately after saving/unsaving.

        // 1. Count saved scholarships (read-only, no tracking needed)
        var savedCount = await _dbContext.SavedScholarships
            .AsNoTracking()
            .CountAsync(s => s.UserId == request.UserId, cancellationToken);

        // 2. Count tracked applications by status
        var statusCounts = await _dbContext.ApplicationTrackers
            .AsNoTracking()
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

        // 4. Get deadlines within 14 days — capped at 10 to prevent unbounded loads (P2 Fix)
        var today = DateTime.UtcNow.Date;
        var in14Days = today.AddDays(14);

        var deadlinesSoon = await _dbContext.ApplicationTrackers
            .AsNoTracking()
            .Include(a => a.Scholarship)
            .Where(a => a.UserId == request.UserId
                && a.Scholarship.Deadline != null
                && a.Scholarship.Deadline >= today
                && a.Scholarship.Deadline <= in14Days)
            .OrderBy(a => a.Scholarship.Deadline)
            .Take(10)
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

        // 5. Build recommended actions as i18n keys — resolved to display text on the frontend
        var recommendedActions = new List<string>();

        var profile = await _dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

        if (profile is null
            || string.IsNullOrWhiteSpace(profile.FieldOfStudy)
            || string.IsNullOrWhiteSpace(profile.Country))
        {
            recommendedActions.Add("action.completeProfile");
        }

        var trackedCount = statusCounts.Sum(s => s.Count);
        if (trackedCount == 0)
        {
            recommendedActions.Add("action.startTracking");
        }

        if (savedCount == 0)
        {
            recommendedActions.Add("action.browseAndSave");
        }

        if (trackedCount > 0)
        {
            var hasReminders = await _dbContext.ApplicationTrackers
                .AnyAsync(a => a.UserId == request.UserId && a.Reminders.Any(), cancellationToken);

            if (!hasReminders)
            {
                recommendedActions.Add("action.setReminders");
            }
        }

        var result = new DashboardSummaryDto
        {
            StatusCounts = statusDict,
            DeadlinesSoon = deadlinesSoon,
            RecommendedActions = recommendedActions
        };

        return result;
    }
}
