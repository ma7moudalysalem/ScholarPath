using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Jobs;

public class DeadlineReminderJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeadlineReminderJob> _logger;

    public DeadlineReminderJob(
        ApplicationDbContext dbContext,
        ILogger<DeadlineReminderJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        var today = DateTime.UtcNow.Date;

        var dueReminders = await _dbContext.ApplicationTrackerReminders
            .Include(r => r.ApplicationTracker)
            .ThenInclude(at => at.Scholarship)
            .Include(r => r.ApplicationTracker.User)
            .Where(r => 
                !r.IsSent &&
                !r.ApplicationTracker.RemindersPaused &&
                !r.ApplicationTracker.IsDeleted &&
                r.ScheduledFor.Date <= today)
            .ToListAsync();

        foreach (var reminder in dueReminders)
        {
            var tracker = reminder.ApplicationTracker;

            _logger.LogInformation(
                "Deadline reminder due: User {UserId}, Scholarship {ScholarshipId}, type: {ReminderType}",
                tracker.UserId, tracker.ScholarshipId, reminder.ReminderType);

            // TODO: Create notification via NotificationService (Module 9)
            
            reminder.IsSent = true;
        }
    }
}
