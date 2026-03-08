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

        var trackers = await _dbContext.ApplicationTrackers
            .Include(a => a.Scholarship)
            .Include(a => a.User)
            .Where(a => !a.RemindersPaused
                && a.RemindersJson != null
                && !a.IsDeleted
                && a.Scholarship.Deadline != null
                && a.Scholarship.Deadline >= today)
            .ToListAsync();

        foreach (var tracker in trackers)
        {
            try
            {
                var reminders = JsonSerializer.Deserialize<ReminderSettings>(
                    tracker.RemindersJson!, JsonOptions);

                if (reminders?.Presets == null || reminders.Presets.Length == 0)
                    continue;

                foreach (var preset in reminders.Presets)
                {
                    var reminderDate = tracker.Scholarship.Deadline!.Value.AddDays(-preset).Date;
                    if (reminderDate == today)
                    {
                        _logger.LogInformation(
                            "Deadline reminder due: User {UserId}, Scholarship {ScholarshipId}, {Days} days before deadline",
                            tracker.UserId, tracker.ScholarshipId, preset);

                        // TODO: Create notification via NotificationService (Module 9)
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid reminder JSON for tracker {TrackerId}", tracker.Id);
            }
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private sealed class ReminderSettings
    {
        public int[]? Presets { get; set; }
        public ChannelSettings? Channels { get; set; }
    }

    private sealed class ChannelSettings
    {
        public bool InApp { get; set; }
        public bool Email { get; set; }
    }
}
