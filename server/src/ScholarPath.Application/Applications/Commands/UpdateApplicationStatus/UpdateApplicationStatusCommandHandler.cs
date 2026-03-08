using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;

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

        tracker.Status = request.Status;
        tracker.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateApplicationStatusResponse>.Success(new UpdateApplicationStatusResponse
        {
            UpdatedAt = tracker.UpdatedAt.Value
        });
    }
}
