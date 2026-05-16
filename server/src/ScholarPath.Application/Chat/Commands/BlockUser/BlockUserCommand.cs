using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Chat.Commands.BlockUser;
[Auditable(AuditAction.Create, "UserBlock",
    TargetIdProperty = nameof(UserIdToBlock),
    SummaryTemplate = "Blocked user {UserIdToBlock}")]
public sealed record BlockUserCommand(
    Guid UserIdToBlock,
    string? Reason) : IRequest<bool>;

public sealed class BlockUserCommandValidator : AbstractValidator<BlockUserCommand>
{
    public BlockUserCommandValidator()
    {
        RuleFor(v => v.UserIdToBlock).NotEmpty();
        RuleFor(v => v.Reason).MaximumLength(500);
    }
}

public sealed class BlockUserCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<BlockUserCommand, bool>
{
    public async Task<bool> Handle(BlockUserCommand request, CancellationToken ct)
    {
        if (request.UserIdToBlock == currentUser.UserId)
            throw new ConflictException("You cannot block yourself.");

        var existingBlock = await db.UserBlocks
            .FirstOrDefaultAsync(b => b.BlockerId == currentUser.UserId && b.BlockedUserId == request.UserIdToBlock, ct);

        if (existingBlock != null)
            return true; // Already blocked

        var block = new UserBlock
        {
            BlockerId = (currentUser.UserId ?? throw new ForbiddenAccessException()),
            BlockedUserId = request.UserIdToBlock,
            Reason = request.Reason
        };

        db.UserBlocks.Add(block);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }
}
