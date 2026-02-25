using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL STUB] Sending email to {To}, Subject: {Subject}, Body length: {BodyLength}",
            to, subject, htmlBody.Length);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string to, string resetLink, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL STUB] Sending password reset email to {To}, Reset link: {ResetLink}",
            to, resetLink);

        return Task.CompletedTask;
    }

    public Task SendUpgradeStatusEmailAsync(string to, string status, string? reason = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL STUB] Sending upgrade status email to {To}, Status: {Status}, Reason: {Reason}",
            to, status, reason ?? "N/A");

        return Task.CompletedTask;
    }
}
