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

        // FR-APP-33/34: external self-tracking only supports Intending / Applied /
        // WaitingResult. Reject anything else so a student can't push an external
        // tracker into an in-app review state (Accepted/UnderReview/Rejected/…).
        if (request.Status is not (ApplicationStatus.Intending
            or ApplicationStatus.Applied
            or ApplicationStatus.WaitingResult))
        {
            throw new ConflictException(
                "External applications can only be set to Intending, Applied, or WaitingResult.");
        }

        application.Status = request.Status;
        application.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }
}
