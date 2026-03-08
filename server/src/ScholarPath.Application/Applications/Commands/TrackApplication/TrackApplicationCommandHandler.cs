using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Commands.TrackApplication;

public class TrackApplicationCommandHandler
    : IRequestHandler<TrackApplicationCommand, Result<TrackApplicationResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public TrackApplicationCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<TrackApplicationResponse>> Handle(
        TrackApplicationCommand request, CancellationToken cancellationToken)
    {
        // Check scholarship exists
        var scholarshipExists = await _dbContext.Scholarships
            .AsNoTracking()
            .AnyAsync(s => s.Id == request.ScholarshipId, cancellationToken);

        if (!scholarshipExists)
            return Result<TrackApplicationResponse>.Failure("Scholarship not found.");

        // Check if already tracked
        var existing = await _dbContext.ApplicationTrackers
            .FirstOrDefaultAsync(at =>
                at.UserId == request.UserId &&
                at.ScholarshipId == request.ScholarshipId,
                cancellationToken);

        if (existing is not null)
        {
            return Result<TrackApplicationResponse>.Success(new TrackApplicationResponse
            {
                Id = existing.Id,
                Status = existing.Status,
                AlreadyExisted = true
            });
        }

        // Create new tracker
        var tracker = new ApplicationTracker
        {
            UserId = request.UserId,
            ScholarshipId = request.ScholarshipId,
            Status = request.Status ?? ApplicationStatus.Planned,
            Notes = request.Notes
        };

        _dbContext.ApplicationTrackers.Add(tracker);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<TrackApplicationResponse>.Success(new TrackApplicationResponse
        {
            Id = tracker.Id,
            Status = tracker.Status,
            AlreadyExisted = false
        });
    }
}
