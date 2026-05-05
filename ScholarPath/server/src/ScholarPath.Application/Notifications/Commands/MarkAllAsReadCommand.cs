using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Application.Common.Attributes;


namespace ScholarPath.Application.Notifications.Commands;

[Auditable(AuditAction.AllNotificationsRead, "Notification")]
public record MarkAllAsReadCommand : IRequest<Unit>;

public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public MarkAllAsReadCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("errors.auth.userNotFound");

        await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTimeOffset.UtcNow),
                cancellationToken);

        return Unit.Value;
    }
}
