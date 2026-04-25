using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;
using ScholarPath.Application.Common.Exceptions;

namespace ScholarPath.Application.Chat.Commands.UnblockUser;

[Auditable(AuditAction.Delete, "UserBlock",
    TargetIdProperty = nameof(BlockedUserId),
    SummaryTemplate = "Unblocked user {BlockedUserId}")]
public sealed record UnblockUserCommand(
    Guid BlockedUserId) : IRequest<bool>;

public sealed class UnblockUserCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<UnblockUserCommand, bool>
{
    public async Task<bool> Handle(UnblockUserCommand request, CancellationToken ct)
    {
        var existingBlock = await db.UserBlocks
            .FirstOrDefaultAsync(b => b.BlockerId == currentUser.UserId && b.BlockedUserId == request.BlockedUserId, ct);

        if (existingBlock != null)
        {
            db.UserBlocks.Remove(existingBlock);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return true;
    }
}
