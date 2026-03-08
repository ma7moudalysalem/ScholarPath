using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Scholarships.Commands.UnsaveScholarship;

public class UnsaveScholarshipCommandHandler : IRequestHandler<UnsaveScholarshipCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;

    public UnsaveScholarshipCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<bool>> Handle(UnsaveScholarshipCommand request, CancellationToken cancellationToken)
    {
        var saved = await _dbContext.SavedScholarships
            .FirstOrDefaultAsync(ss => ss.UserId == request.UserId && ss.ScholarshipId == request.ScholarshipId,
                cancellationToken);

        if (saved is not null)
        {
            _dbContext.SavedScholarships.Remove(saved);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<bool>.Success(true);
    }
}
