using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;

using ScholarPath.Domain.Entities;

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
            .Include(a => a.ChecklistItems)
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == request.UserId, cancellationToken);

        if (tracker is null)
            return Result<UpdateApplicationChecklistResponse>.Failure("Application not found or access denied");

        _dbContext.ApplicationTrackerChecklistItems.RemoveRange(tracker.ChecklistItems);
        
        var newItems = request.Items.Select(i => new ApplicationTrackerChecklistItem
        {
            ApplicationTrackerId = tracker.Id,
            Text = i.Text,
            IsChecked = i.IsChecked
        }).ToList();

        _dbContext.ApplicationTrackerChecklistItems.AddRange(newItems);
        
        tracker.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateApplicationChecklistResponse>.Success(new UpdateApplicationChecklistResponse
        {
            UpdatedAt = tracker.UpdatedAt.Value
        });
    }
}
