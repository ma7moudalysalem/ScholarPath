using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Applications.Commands.UpdateReminders;

public class UpdateRemindersCommandHandler
    : IRequestHandler<UpdateRemindersCommand, Result<UpdateRemindersResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateRemindersCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<UpdateRemindersResponse>> Handle(
        UpdateRemindersCommand request, CancellationToken cancellationToken)
    {
        var tracker = await _dbContext.ApplicationTrackers
            .Include(a => a.Reminders)
            .FirstOrDefaultAsync(
                a => a.Id == request.ApplicationId && a.UserId == request.UserId,
                cancellationToken);

        if (tracker is null)
            return Result<UpdateRemindersResponse>.Failure("errors.applications.notFound");

        // Note: The original implementation also saved channel preferences (email/inApp) in the same JSON.
        // If we strictly follow the audit report "D2" to extract arrays to related entities, 
        // we extract the presets as related entities. The channel preferences might need to be added
        // as dedicated boolean columns on the ApplicationTracker entity if they were in the JSON.
        // For now, let's add the presets as reminder entities, but we don't have columns for channels yet.
        // We will store presets as new ApplicationTrackerReminder entities.
        
        _dbContext.ApplicationTrackerReminders.RemoveRange(tracker.Reminders);

        var newReminders = new List<ApplicationTrackerReminder>();
        
        // Calculate ScheduledFor based on Application Deadline and Presets (days before)
        // Since we don't eagerly load scholarship deadline here, we might just store the preset day count in ReminderType
        // and calculate the exact date in a background job or fetch the deadline first.
        // Let's load the deadline.
        var scholarship = await _dbContext.Scholarships
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == tracker.ScholarshipId, cancellationToken);
            
        foreach(var daysBefore in request.Presets)
        {
            var scheduledFor = scholarship?.Deadline?.AddDays(-daysBefore) ?? DateTime.UtcNow.AddDays(1);
            
            newReminders.Add(new ApplicationTrackerReminder
            {
                ApplicationTrackerId = tracker.Id,
                ReminderType = $"{daysBefore} Days Before",
                ScheduledFor = scheduledFor,
                IsSent = false
            });
        }
        
        _dbContext.ApplicationTrackerReminders.AddRange(newReminders);

        tracker.RemindersPaused = request.Paused;
        tracker.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateRemindersResponse>.Success(new UpdateRemindersResponse
        {
            UpdatedAt = tracker.UpdatedAt.Value
        });
    }
}
