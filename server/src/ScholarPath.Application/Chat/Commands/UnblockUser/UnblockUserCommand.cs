using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Chat.Commands.UnblockUser;

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
