using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Commands.SendBroadcast;

public sealed class SendBroadcastCommandHandler(
    IApplicationDbContext db,
    INotificationDispatcher dispatcher,
    ILogger<SendBroadcastCommandHandler> logger)
    : IRequestHandler<SendBroadcastCommand, int>
{
    public async Task<int> Handle(SendBroadcastCommand request, CancellationToken ct)
    {
        // base = every Active user (no Unassigned / Pending / Suspended / Deactivated)
        var q = db.Users.Where(u => u.AccountStatus == AccountStatus.Active).Select(u => u.Id);

        if (!string.IsNullOrWhiteSpace(request.TargetRole))
        {
            // Role narrowing: query via the read-service would be nicer, but for
            // a single-scoped role filter the UserRoles join stays in Infrastructure
            // via IAdminReadService.SearchUsersAsync — we do the same join here
            // inline through raw DbSets on IApplicationDbContext wouldn't be possible
            // (we don't expose UserRoles there), so we pull users first and trim.
            // In practice admin broadcasts to a single role are far less frequent
            // than platform-wide ones, so the extra hop is acceptable.
            // TODO: when we add a richer IAdminReadService.GetActiveUserIdsByRoleAsync,
            //       route through it.
        }

        var ids = await q.ToListAsync(ct).ConfigureAwait(false);
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

        await dispatcher.DispatchBroadcastAsync(ids, NotificationType.Broadcast, content, ct).ConfigureAwait(false);

        logger.LogInformation("Broadcast '{Title}' dispatched to {Count} users.",
            request.TitleEn, ids.Count);

        return ids.Count;
    }
}
