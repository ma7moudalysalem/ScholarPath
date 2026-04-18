using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Audit.Commands.RequestDataExport;

public sealed class RequestDataExportCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService clock)
    : IRequestHandler<RequestDataExportCommand, DataRequestDto>
{
    public async Task<DataRequestDto> Handle(RequestDataExportCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        // one in-flight export per user at a time
        var existing = await db.UserDataRequests
            .Where(r => r.UserId == userId
                && r.Type == UserDataRequestType.Export
                && (r.Status == UserDataRequestStatus.Pending || r.Status == UserDataRequestStatus.Processing))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            throw new ConflictException("You already have a data export in progress.");
        }

        var entity = new UserDataRequest
        {
            UserId = userId,
            Type = UserDataRequestType.Export,
            Status = UserDataRequestStatus.Pending,
            RequestedAt = clock.UtcNow,
            CreatedAt = clock.UtcNow,
        };
        db.UserDataRequests.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // The Hangfire DataExportJob picks up pending export rows. We don't
        // enqueue here directly — a recurring job sweeps pending requests.

        return ToDto(entity);
    }

    private static DataRequestDto ToDto(UserDataRequest r) =>
        new(r.Id, r.Type, r.Status, r.RequestedAt, r.ScheduledProcessAt,
            r.CompletedAt, r.CancelledAt, r.DownloadUrl, r.DownloadExpiresAt);
}
