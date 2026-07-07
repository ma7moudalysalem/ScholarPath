using MediatR;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Notifications.Commands.SendTestNotification;

/// <summary>
/// Sends the current user a one-off test notification (FR-228) so they can verify
/// their in-app channel actually works — fired from the notification-preferences
/// page. Bypasses mute / quiet hours (the whole point is to see it now).
/// </summary>
public sealed record SendTestNotificationCommand : IRequest;

public sealed class SendTestNotificationCommandHandler(
    ICurrentUserService currentUser,
    INotificationDispatcher dispatcher) : IRequestHandler<SendTestNotificationCommand>
{
    public async Task Handle(SendTestNotificationCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        await dispatcher.DispatchAsync(
            userId,
            NotificationType.SystemTest,
            new NotificationParams(),
            deepLink: "/notifications",
            idempotencyKey: null,
            ct);
    }
}
