using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Applications.Commands.UpdateApplicationNotes;

public class UpdateApplicationNotesCommandHandler
    : IRequestHandler<UpdateApplicationNotesCommand, Result<UpdateApplicationNotesResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateApplicationNotesCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<UpdateApplicationNotesResponse>> Handle(
        UpdateApplicationNotesCommand request, CancellationToken cancellationToken)
    {
        var tracker = await _dbContext.ApplicationTrackers
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == request.UserId, cancellationToken);

        if (tracker is null)
            return Result<UpdateApplicationNotesResponse>.Failure("Application not found or access denied");

        tracker.Notes = request.Notes;
        tracker.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateApplicationNotesResponse>.Success(new UpdateApplicationNotesResponse
        {
            UpdatedAt = tracker.UpdatedAt.Value
        });
    }
}
