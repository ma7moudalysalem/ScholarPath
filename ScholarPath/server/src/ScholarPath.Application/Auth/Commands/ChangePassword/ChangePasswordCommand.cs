using MediatR;
using ScholarPath.Application.Common.Attributes;

namespace ScholarPath.Application.Auth.Commands.ChangePassword;

[Auditable(AuditAction.PasswordChanged, "User")]
public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest<Unit>;
    
