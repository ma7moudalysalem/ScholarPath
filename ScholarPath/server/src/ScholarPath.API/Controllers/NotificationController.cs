using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Notifications.Queries.GetNotifications;
using ScholarPath.Application.Notifications.Commands;
using ScholarPath.Application.Notifications.DTOs;

namespace ScholarPath.API.Controllers;

[Route("api/v{version:apiVersion}/notifications")]
[Authorize]
public class NotificationController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<PaginatedNotificationResponse>> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetNotificationsQuery(page, pageSize);
        var result = await Mediator.Send(query, cancellationToken);
        return OkResult(result);
    }

    [HttpPut("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken cancellationToken)
    {
        var command = new MarkAsReadCommand(notificationId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var command = new MarkAllAsReadCommand();
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
