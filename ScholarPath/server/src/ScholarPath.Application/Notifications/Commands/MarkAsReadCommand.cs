using MediatR;

namespace ScholarPath.Application.Notifications.Commands;

public record MarkAsReadCommand(Guid NotificationId) : IRequest<Unit>;

public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Unit>
{
    private readonly Common.Interfaces.IApplicationDbContext _dbContext;
    private readonly Domain.Interfaces.ICurrentUserService _currentUserService;

    public MarkAsReadCommandHandler(
        Common.Interfaces.IApplicationDbContext dbContext,
        Domain.Interfaces.ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("errors.auth.userNotFound");

        var notification = await _dbContext.Notifications
            .FindAsync(new object[] { request.NotificationId }, cancellationToken)
            ?? throw new InvalidOperationException("errors.notification.notFound");

        if (notification.UserId != userId)
        {
            throw new UnauthorizedAccessException("errors.auth.forbidden");
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
