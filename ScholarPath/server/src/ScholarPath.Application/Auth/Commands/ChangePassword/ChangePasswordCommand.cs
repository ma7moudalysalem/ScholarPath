using MediatR;

namespace ScholarPath.Application.Auth.Commands.ChangePassword;

public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest<Unit>;