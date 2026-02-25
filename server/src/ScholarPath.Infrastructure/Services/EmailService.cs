using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _emailSettings;

    public EmailService(ILogger<EmailService> logger, IOptions<EmailSettings> emailSettings)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
    }

    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL STUB] Sending email from {Sender} to {To}, Subject: {Subject}, Body length: {BodyLength}",
            _emailSettings.SenderEmail, to, subject, htmlBody.Length);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string to, string resetLink, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL STUB] Sending password reset email to {To}, Reset link: {ResetLink}",
            to, resetLink);

        return Task.CompletedTask;
    }

    public Task SendUpgradeStatusEmailAsync(string to, UpgradeRequestStatus status, string? reason = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL STUB] Sending upgrade status email to {To}, Status: {Status}, Reason: {Reason}",
            to, status, reason ?? "N/A");

        return Task.CompletedTask;
    }
}
