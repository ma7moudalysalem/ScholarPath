using MediatR;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Application.Common;

namespace ScholarPath.Application.Applications.Commands.UpdateReminders;

public record UpdateRemindersCommand(
    Guid ApplicationId,
    Guid UserId,
    int[] Presets,
    ReminderChannels Channels,
    bool Paused
) : IRequest<Result<UpdateRemindersResponse>>;

public class UpdateRemindersResponse
{
    public DateTime UpdatedAt { get; set; }
}
