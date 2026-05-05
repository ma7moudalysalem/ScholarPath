using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Application.Notifications.Queries.GetUnreadCount;

namespace ScholarPath.Application.Notifications.Queries.GetUnreadCount;

public record GetUnreadCountQuery : IRequest<int>;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetUnreadCountQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("errors.auth.userNotFound");

        return await _dbContext.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }
}
