using MediatR;

namespace ScholarPath.Application.Notifications.Commands.MarkAllAsRead;

/// <summary>Marks every unread notification of the current user as read; returns the count.</summary>
public sealed record MarkAllAsReadCommand : IRequest<int>;
