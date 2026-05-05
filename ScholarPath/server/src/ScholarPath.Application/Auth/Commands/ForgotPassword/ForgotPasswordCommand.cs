using MediatR;
using ScholarPath.Application.Common.Attributes;

namespace ScholarPath.Application.Auth.Commands.ForgotPassword;

[Auditable(AuditAction.PasswordResetRequested, "User")]
public record ForgotPasswordCommand(string Email) : IRequest<Unit>;
