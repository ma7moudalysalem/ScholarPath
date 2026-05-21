using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Commands.SendBroadcast;

public sealed class SendBroadcastCommandHandler(
    IApplicationDbContext db,
    IAdminReadService adminRead,
    INotificationDispatcher dispatcher,
    ILogger<SendBroadcastCommandHandler> logger)
    : IRequestHandler<SendBroadcastCommand, int>
{
    public async Task<int> Handle(SendBroadcastCommand request, CancellationToken ct)
    {
        List<Guid> ids;

        if (!string.IsNullOrWhiteSpace(request.TargetRole))
        {
            // Role-scoped broadcast: use the admin read service which has access to
            // Identity join tables (UserRoles) that aren't exposed on IApplicationDbContext.
            ids = await adminRead.GetActiveUserIdsByRoleAsync(request.TargetRole, ct).ConfigureAwait(false);
        }
        else
        {
            // Platform-wide broadcast: all Active users.
            ids = await db.Users
                .Where(u => u.AccountStatus == AccountStatus.Active)
                .Select(u => u.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        if (ids.Count == 0)
        {
            logger.LogInformation("Broadcast requested but zero Active users matched.");
            return 0;
        }

        var content = new NotificationContent(
            TitleEn: request.TitleEn,
            TitleAr: request.TitleAr,
            BodyEn: request.BodyEn,
            BodyAr: request.BodyAr);

        await dispatcher.DispatchBroadcastAsync(
            ids, NotificationType.Broadcast,
            new NotificationParams { RawContent = content }, ct).ConfigureAwait(false);

        logger.LogInformation("Broadcast '{Title}' dispatched to {Count} users.",
            request.TitleEn, ids.Count);

        return ids.Count;
    }
}
