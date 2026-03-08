using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Applications.Commands.DeleteApplication;

public class DeleteApplicationCommandHandler
    : IRequestHandler<DeleteApplicationCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteApplicationCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<bool>> Handle(
        DeleteApplicationCommand request, CancellationToken cancellationToken)
    {
        var tracker = await _dbContext.ApplicationTrackers
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == request.UserId, cancellationToken);

        if (tracker is null)
            return Result<bool>.Failure("Application not found or access denied");

        // Soft delete - EF handles via ISoftDeletable interceptor
        tracker.IsDeleted = true;
        tracker.DeletedAt = DateTime.UtcNow;
        tracker.DeletedBy = request.UserId.ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
