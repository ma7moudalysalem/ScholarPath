using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.Commands.ResetPassword;

[Auditable(AuditAction.PasswordReset, "User")]
public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest;
