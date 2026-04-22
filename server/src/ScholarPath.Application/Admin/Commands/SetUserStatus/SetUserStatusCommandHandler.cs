using MediatR;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Admin.Commands.SetUserStatus;

public sealed class SetUserStatusCommandHandler(
    IUserAdministration admin,
    ILogger<SetUserStatusCommandHandler> logger)
    : IRequestHandler<SetUserStatusCommand, bool>
{
    public async Task<bool> Handle(SetUserStatusCommand request, CancellationToken ct)
    {
        var ok = await admin.SetAccountStatusAsync(
            request.UserId, request.NewStatus, request.Reason, ct).ConfigureAwait(false);

        if (!ok)
        {
            logger.LogWarning("SetUserStatus no-op: user {UserId} not found.", request.UserId);
            throw new NotFoundException("User", request.UserId);
        }

        logger.LogInformation("Admin set user {UserId} status → {Status} (reason: {Reason})",
            request.UserId, request.NewStatus, request.Reason ?? "<none>");

        return true;
    }
}
