using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Applications.Commands.UpdateReminders;

public class UpdateRemindersCommandHandler
    : IRequestHandler<UpdateRemindersCommand, Result<UpdateRemindersResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public UpdateRemindersCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<UpdateRemindersResponse>> Handle(
        UpdateRemindersCommand request, CancellationToken cancellationToken)
    {
        var tracker = await _dbContext.ApplicationTrackers
            .FirstOrDefaultAsync(
                a => a.Id == request.ApplicationId && a.UserId == request.UserId,
                cancellationToken);

        if (tracker is null)
            return Result<UpdateRemindersResponse>.Failure("errors.applications.notFound");

        var remindersPayload = new
        {
            presets = request.Presets,
            channels = new
            {
                inApp = request.Channels.InApp,
                email = request.Channels.Email
            }
        };

        tracker.RemindersJson = JsonSerializer.Serialize(remindersPayload, JsonOptions);
        tracker.RemindersPaused = request.Paused;
        tracker.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateRemindersResponse>.Success(new UpdateRemindersResponse
        {
            UpdatedAt = tracker.UpdatedAt.Value
        });
    }
}
