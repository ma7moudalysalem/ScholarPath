using MediatR;

namespace ScholarPath.Application.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<Unit>;