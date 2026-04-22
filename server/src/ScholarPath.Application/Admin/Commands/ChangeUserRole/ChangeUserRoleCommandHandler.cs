using MediatR;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Admin.Commands.ChangeUserRole;

public sealed class ChangeUserRoleCommandHandler(
    IUserAdministration admin,
    ILogger<ChangeUserRoleCommandHandler> logger)
    : IRequestHandler<ChangeUserRoleCommand, bool>
{
    public async Task<bool> Handle(ChangeUserRoleCommand request, CancellationToken ct)
    {
        var ok = request.Operation switch
        {
            RoleOp.Add    => await admin.AddRoleAsync(request.UserId, request.Role, ct).ConfigureAwait(false),
            RoleOp.Remove => await admin.RemoveRoleAsync(request.UserId, request.Role, ct).ConfigureAwait(false),
            _             => false,
        };

        if (!ok)
        {
            var verb = request.Operation == RoleOp.Add ? "add" : "remove";
            throw new ConflictException(
                $"Could not {verb} role '{request.Role}' for user {request.UserId}.");
        }

        logger.LogInformation("Admin {Op} role {Role} on {UserId}",
            request.Operation, request.Role, request.UserId);
        return true;
    }
}
