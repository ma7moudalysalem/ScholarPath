using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Notifications.Commands.MarkAllAsRead;
using ScholarPath.Application.Notifications.Commands.MarkAsRead;
using ScholarPath.Application.Notifications.DTOs;
using ScholarPath.Application.Notifications.Queries.GetNotifications;
using ScholarPath.Application.Notifications.Queries.GetUnreadCount;

namespace ScholarPath.API.Controllers;

/// <summary>In-app notifications (PB-010). All routes require authentication.</summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public sealed class NotificationController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(NotificationsPageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationsPageDto>> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new GetNotificationsQuery(page, pageSize), ct));

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken ct)
        => Ok(await mediator.Send(new GetUnreadCountQuery(), ct));

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        await mediator.Send(new MarkAsReadCommand(id), ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var marked = await mediator.Send(new MarkAllAsReadCommand(), ct);
        return Ok(new { marked });
    }
}
