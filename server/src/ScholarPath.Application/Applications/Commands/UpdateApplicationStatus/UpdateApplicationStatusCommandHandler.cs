using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Commands.UpdateApplicationStatus;

public class UpdateApplicationStatusCommandHandler
    : IRequestHandler<UpdateApplicationStatusCommand, Result<UpdateApplicationStatusResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateApplicationStatusCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<UpdateApplicationStatusResponse>> Handle(
        UpdateApplicationStatusCommand request, CancellationToken cancellationToken)
    {
        var tracker = await _dbContext.ApplicationTrackers
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == request.UserId, cancellationToken);

        if (tracker is null)
            return Result<UpdateApplicationStatusResponse>.Failure("Application not found or access denied");

        if (!IsValidTransition(tracker.Status, request.Status))
        {
            return Result<UpdateApplicationStatusResponse>.Failure($"Invalid status transition from {tracker.Status} to {request.Status}");
        }

        tracker.Status = request.Status;
        tracker.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateApplicationStatusResponse>.Success(new UpdateApplicationStatusResponse
        {
            UpdatedAt = tracker.UpdatedAt.Value
        });
    }

    private static bool IsValidTransition(ApplicationStatus current, ApplicationStatus next)
    {
        if (current == next) return true;

        return current switch
        {
            ApplicationStatus.Planned => true,
            ApplicationStatus.Applied => next is ApplicationStatus.Pending or ApplicationStatus.Accepted or ApplicationStatus.Rejected,
            ApplicationStatus.Pending => next is ApplicationStatus.Accepted or ApplicationStatus.Rejected,
            ApplicationStatus.Accepted => false,
            ApplicationStatus.Rejected => false,
            _ => false
        };
    }
}
