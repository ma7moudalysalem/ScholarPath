using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Applications.Commands.UpdateApplicationChecklist;

public class UpdateApplicationChecklistCommandHandler
    : IRequestHandler<UpdateApplicationChecklistCommand, Result<UpdateApplicationChecklistResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateApplicationChecklistCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<UpdateApplicationChecklistResponse>> Handle(
        UpdateApplicationChecklistCommand request, CancellationToken cancellationToken)
    {
        var tracker = await _dbContext.ApplicationTrackers
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == request.UserId, cancellationToken);

        if (tracker is null)
            return Result<UpdateApplicationChecklistResponse>.Failure("Application not found or access denied");

        tracker.ChecklistJson = JsonSerializer.Serialize(request.Items);
        tracker.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateApplicationChecklistResponse>.Success(new UpdateApplicationChecklistResponse
        {
            UpdatedAt = tracker.UpdatedAt.Value
        });
    }
}
