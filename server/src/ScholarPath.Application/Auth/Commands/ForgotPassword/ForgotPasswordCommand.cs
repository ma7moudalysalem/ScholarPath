using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.Commands.ForgotPassword;

[Auditable(AuditAction.PasswordReset, "User")]
public sealed record ForgotPasswordCommand(string Email) : IRequest;
