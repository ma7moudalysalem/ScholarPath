using MediatR;

namespace ScholarPath.Application.Notifications.Queries.GetUnreadCount;

public sealed record GetUnreadCountQuery : IRequest<int>;
