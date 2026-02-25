using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string to, string resetLink, CancellationToken cancellationToken = default);
    Task SendUpgradeStatusEmailAsync(string to, UpgradeRequestStatus status, string? reason = null, CancellationToken cancellationToken = default);
}
