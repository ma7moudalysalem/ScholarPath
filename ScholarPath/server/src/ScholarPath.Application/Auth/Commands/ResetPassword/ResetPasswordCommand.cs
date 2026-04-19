using MediatR;

namespace ScholarPath.Application.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(
    string Token,
    string NewPassword) : IRequest<Unit>;