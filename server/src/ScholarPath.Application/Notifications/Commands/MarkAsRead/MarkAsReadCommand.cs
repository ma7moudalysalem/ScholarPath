using MediatR;

namespace ScholarPath.Application.Notifications.Commands.MarkAsRead;

public sealed record MarkAsReadCommand(Guid NotificationId) : IRequest;
