using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Scholarships.Commands.SaveScholarship;

public class SaveScholarshipCommandHandler : IRequestHandler<SaveScholarshipCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;

    public SaveScholarshipCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<bool>> Handle(SaveScholarshipCommand request, CancellationToken cancellationToken)
    {
        var scholarshipExists = await _dbContext.Scholarships
            .AnyAsync(s => s.Id == request.ScholarshipId, cancellationToken);

        if (!scholarshipExists)
            return Result<bool>.Failure("Scholarship not found.");

        var alreadySaved = await _dbContext.SavedScholarships
            .AnyAsync(ss => ss.UserId == request.UserId && ss.ScholarshipId == request.ScholarshipId,
                cancellationToken);

        if (!alreadySaved)
        {
            var savedScholarship = new SavedScholarship
            {
                UserId = request.UserId,
                ScholarshipId = request.ScholarshipId
            };
            
            try
            {
                _dbContext.SavedScholarships.Add(savedScholarship);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Unique constraint violation means it was already saved by a concurrent request.
                // We can safely treat this as a success.
            }
        }

        return Result<bool>.Success(true);
    }
}
