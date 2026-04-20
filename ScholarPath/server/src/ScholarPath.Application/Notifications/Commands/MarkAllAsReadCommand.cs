using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ScholarPath.Application.Notifications.Commands;

public record MarkAllAsReadCommand : IRequest<Unit>;

public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, Unit>
{
    private readonly Common.Interfaces.IApplicationDbContext _dbContext;
    private readonly Domain.Interfaces.ICurrentUserService _currentUserService;

    public MarkAllAsReadCommandHandler(
        Common.Interfaces.IApplicationDbContext dbContext,
        Domain.Interfaces.ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("errors.auth.userNotFound");

        var unreadNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
