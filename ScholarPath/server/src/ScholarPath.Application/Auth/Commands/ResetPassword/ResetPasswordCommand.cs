using MediatR;
using ScholarPath.Application.Common.Attributes;

namespace ScholarPath.Application.Auth.Commands.ResetPassword;

[Auditable(AuditAction.PasswordResetCompleted, "User")]
public record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword) : IRequest<Unit>;
    
