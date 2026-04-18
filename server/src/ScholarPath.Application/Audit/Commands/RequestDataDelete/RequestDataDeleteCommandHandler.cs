using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Audit.Commands.RequestDataDelete;

public sealed class RequestDataDeleteCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService clock)
    : IRequestHandler<RequestDataDeleteCommand, DataRequestDto>
{
    // 30-day cooling-off window before the account is actually deleted.
    // Student can cancel any time during this period via CancelDataDeleteCommand.
    private const int CoolingOffDays = 30;

    public async Task<DataRequestDto> Handle(RequestDataDeleteCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var existing = await db.UserDataRequests
            .Where(r => r.UserId == userId
                && r.Type == UserDataRequestType.Delete
                && (r.Status == UserDataRequestStatus.Pending || r.Status == UserDataRequestStatus.Processing))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            throw new ConflictException("An account deletion request is already pending.");
        }

        var now = clock.UtcNow;
        var entity = new UserDataRequest
        {
            UserId = userId,
            Type = UserDataRequestType.Delete,
            Status = UserDataRequestStatus.Pending,
            RequestedAt = now,
            ScheduledProcessAt = now.AddDays(CoolingOffDays),
            CreatedAt = now,
        };
        db.UserDataRequests.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new DataRequestDto(entity.Id, entity.Type, entity.Status, entity.RequestedAt,
            entity.ScheduledProcessAt, entity.CompletedAt, entity.CancelledAt,
            entity.DownloadUrl, entity.DownloadExpiresAt);
    }
}
