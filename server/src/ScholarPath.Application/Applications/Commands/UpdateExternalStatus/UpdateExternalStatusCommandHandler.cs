using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Commands.UpdateExternalStatus;

public sealed class UpdateExternalStatusCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateExternalStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateExternalStatusCommand request, CancellationToken ct)
    {
        var application = await db.Applications
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.ApplicationTracker), request.Id);

        if (application.StudentId != currentUser.UserId)
        {
            throw new ForbiddenAccessException();
        }

        if (application.Mode != ApplicationMode.External)
        {
            throw new ConflictException("Only external applications can be manually updated by the student.");
        }

        application.Status = request.Status;
        application.LastModifiedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }
}
