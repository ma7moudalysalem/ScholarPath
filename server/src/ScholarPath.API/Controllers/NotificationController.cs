using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Notifications.Commands.MarkAllAsRead;
using ScholarPath.Application.Notifications.Commands.MarkAsRead;
using ScholarPath.Application.Notifications.Commands.SendTestNotification;
using ScholarPath.Application.Notifications.Commands.UpdateNotificationPreference;
using ScholarPath.Application.Notifications.Commands.UpdateNotificationSettings;
using ScholarPath.Application.Notifications.DTOs;
using ScholarPath.Application.Notifications.Queries.GetNotificationPreferences;
using ScholarPath.Application.Notifications.Queries.GetNotifications;
using ScholarPath.Application.Notifications.Queries.GetUnreadCount;
using ScholarPath.Domain.Enums;

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

    /// <summary>
    /// Returns the current user's notification-delivery preferences (FR-228) — one
    /// entry per notification type × channel; unconfigured pairs report as enabled.
    /// </summary>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(NotificationPreferencesDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationPreferencesDto>> GetPreferences(CancellationToken ct)
        => Ok(await mediator.Send(new GetNotificationPreferencesQuery(), ct));

    /// <summary>
    /// Enables or disables one delivery channel for one notification type for the
    /// current user (FR-228).
    /// </summary>
    [HttpPut("preferences")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePreference(
        [FromBody] UpdateNotificationPreferenceRequest request,
        CancellationToken ct)
    {
        await mediator.Send(
            new UpdateNotificationPreferenceCommand(request.Type, request.Channel, request.IsEnabled),
            ct);
        return NoContent();
    }

    /// <summary>
    /// Updates the current user's global "do not disturb" settings — mute-all and
    /// quiet hours (FR-228).
    /// </summary>
    [HttpPut("settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateNotificationSettingsRequest request,
        CancellationToken ct)
    {
        await mediator.Send(new UpdateNotificationSettingsCommand(
            request.Muted, request.QuietHoursEnabled,
            request.QuietStart, request.QuietEnd, request.QuietTimezone), ct);
        return NoContent();
    }

    /// <summary>Sends the current user a one-off test notification (FR-228).</summary>
    [HttpPost("test")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SendTest(CancellationToken ct)
    {
        await mediator.Send(new SendTestNotificationCommand(), ct);
        return NoContent();
    }
}

/// <summary>Body of <c>PUT /api/notifications/preferences</c> (FR-228).</summary>
public sealed record UpdateNotificationPreferenceRequest(
    NotificationType Type,
    NotificationChannel Channel,
    bool IsEnabled);

/// <summary>Body of <c>PUT /api/notifications/settings</c> (FR-228). Quiet times are
/// "HH:mm" in the supplied IANA <c>QuietTimezone</c>.</summary>
public sealed record UpdateNotificationSettingsRequest(
    bool Muted,
    bool QuietHoursEnabled,
    string? QuietStart,
    string? QuietEnd,
    string? QuietTimezone);
