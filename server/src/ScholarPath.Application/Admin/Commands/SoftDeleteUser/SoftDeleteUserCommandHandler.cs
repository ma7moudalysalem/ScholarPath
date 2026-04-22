using MediatR;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Admin.Commands.SoftDeleteUser;

public sealed class SoftDeleteUserCommandHandler(
    IUserAdministration admin,
    ILogger<SoftDeleteUserCommandHandler> logger)
    : IRequestHandler<SoftDeleteUserCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteUserCommand request, CancellationToken ct)
    {
        var ok = await admin.SoftDeleteAsync(request.UserId, ct).ConfigureAwait(false);
        if (!ok)
        {
            throw new NotFoundException("User", request.UserId);
        }

        logger.LogInformation("Admin soft-deleted user {UserId}. Reason: {Reason}",
            request.UserId, request.Reason ?? "<none>");

        return true;
    }
}
