using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Settings;
using SendGrid;
using SendGrid.Helpers.Mail;

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

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var provider = _emailSettings.Provider.ToLowerInvariant();

        switch (provider)
        {
            case "smtp":
                await SendViaSmtpAsync(to, subject, htmlBody, cancellationToken);
                break;
            case "sendgrid":
                await SendViaSendGridAsync(to, subject, htmlBody, cancellationToken);
                break;
            case "none":
            case "":
                _logger.LogInformation(
                    "[EMAIL] Provider is 'none' — skipping email to {To}, Subject: {Subject}",
                    to, subject);
                break;
            default:
                _logger.LogWarning(
                    "[EMAIL] Unknown provider '{Provider}' — skipping email to {To}, Subject: {Subject}",
                    provider, to, subject);
                break;
        }
    }

    public async Task SendPasswordResetEmailAsync(string to, string resetLink, CancellationToken cancellationToken = default)
    {
        const string subject = "ScholarPath — Password Reset";
        var htmlBody = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2 style="color: #2563eb;">Password Reset</h2>
                <p>You requested a password reset for your ScholarPath account.</p>
                <p>Click the link below to reset your password:</p>
                <p><a href="{resetLink}" style="display: inline-block; padding: 12px 24px; background-color: #2563eb; color: #ffffff; text-decoration: none; border-radius: 6px;">Reset Password</a></p>
                <p style="color: #6b7280; font-size: 14px;">If you did not request this, please ignore this email.</p>
                <hr style="border: none; border-top: 1px solid #e5e7eb;" />
                <p style="color: #9ca3af; font-size: 12px;">ScholarPath Team</p>
            </div>
            """;

        await SendEmailAsync(to, subject, htmlBody, cancellationToken);
    }

    public async Task SendUpgradeStatusEmailAsync(string to, UpgradeRequestStatus status, string? reason = null, CancellationToken cancellationToken = default)
    {
        var (subject, body) = status switch
        {
            UpgradeRequestStatus.Approved => (
                "ScholarPath — Upgrade Approved",
                "<p>Your upgrade request has been <strong>approved</strong>. You can now access your new role features.</p>"),
            UpgradeRequestStatus.Rejected => (
                "ScholarPath — Upgrade Rejected",
                $"<p>Your upgrade request has been <strong>rejected</strong>.</p>{(reason != null ? $"<p>Reason: {reason}</p>" : "")}"),
            UpgradeRequestStatus.NeedsMoreInfo => (
                "ScholarPath — Additional Information Needed",
                $"<p>Your upgrade request requires additional information.</p>{(reason != null ? $"<p>Details: {reason}</p>" : "")}"),
            _ => (
                "ScholarPath — Upgrade Request Update",
                $"<p>Your upgrade request status has been updated to: <strong>{status}</strong>.</p>")
        };

        var htmlBody = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2 style="color: #2563eb;">Upgrade Request Update</h2>
                {body}
                <hr style="border: none; border-top: 1px solid #e5e7eb;" />
                <p style="color: #9ca3af; font-size: 12px;">ScholarPath Team</p>
            </div>
            """;

        await SendEmailAsync(to, subject, htmlBody, cancellationToken);
    }

    public async Task SendUpgradeApprovedEmailAsync(string to, string userName, UserRole role, string language = "en", CancellationToken cancellationToken = default)
    {
        var isArabic = language.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        var dir = isArabic ? "rtl" : "ltr";

        var subject = isArabic
            ? "ScholarPath — تمت الموافقة على طلب الترقية"
            : "ScholarPath — Upgrade Approved";

        var roleName = role.ToString();
        var htmlBody = isArabic
            ? $"""
              <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; direction: {dir}; text-align: right;">
                  <h2 style="color: #2563eb;">تمت الموافقة على طلب الترقية</h2>
                  <p>مرحباً {userName}،</p>
                  <p>تمت الموافقة على طلب ترقيتك إلى <strong>{roleName}</strong>. يمكنك الآن الوصول إلى ميزات دورك الجديد.</p>
                  <hr style="border: none; border-top: 1px solid #e5e7eb;" />
                  <p style="color: #9ca3af; font-size: 12px;">فريق ScholarPath</p>
              </div>
              """
            : $"""
              <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; direction: {dir};">
                  <h2 style="color: #2563eb;">Upgrade Request Approved</h2>
                  <p>Hello {userName},</p>
                  <p>Your upgrade request to <strong>{roleName}</strong> has been approved. You can now access your new role features.</p>
                  <hr style="border: none; border-top: 1px solid #e5e7eb;" />
                  <p style="color: #9ca3af; font-size: 12px;">ScholarPath Team</p>
              </div>
              """;

        await SendEmailAsync(to, subject, htmlBody, cancellationToken);
    }

    public async Task SendUpgradeRejectedEmailAsync(string to, string userName, string? reasons = null, string language = "en", CancellationToken cancellationToken = default)
    {
        var isArabic = language.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        var dir = isArabic ? "rtl" : "ltr";

        var subject = isArabic
            ? "ScholarPath — تم رفض طلب الترقية"
            : "ScholarPath — Upgrade Rejected";

        var reasonSection = string.IsNullOrWhiteSpace(reasons)
            ? ""
            : isArabic
                ? $"<p><strong>السبب:</strong> {reasons}</p>"
                : $"<p><strong>Reason:</strong> {reasons}</p>";

        var htmlBody = isArabic
            ? $"""
              <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; direction: {dir}; text-align: right;">
                  <h2 style="color: #dc2626;">تم رفض طلب الترقية</h2>
                  <p>مرحباً {userName}،</p>
                  <p>للأسف، تم رفض طلب الترقية الخاص بك.</p>
                  {reasonSection}
                  <hr style="border: none; border-top: 1px solid #e5e7eb;" />
                  <p style="color: #9ca3af; font-size: 12px;">فريق ScholarPath</p>
              </div>
              """
            : $"""
              <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; direction: {dir};">
                  <h2 style="color: #dc2626;">Upgrade Request Rejected</h2>
                  <p>Hello {userName},</p>
                  <p>Unfortunately, your upgrade request has been rejected.</p>
                  {reasonSection}
                  <hr style="border: none; border-top: 1px solid #e5e7eb;" />
                  <p style="color: #9ca3af; font-size: 12px;">ScholarPath Team</p>
              </div>
              """;

        await SendEmailAsync(to, subject, htmlBody, cancellationToken);
    }

    public async Task SendNeedsMoreInfoEmailAsync(string to, string userName, string? notes = null, string language = "en", CancellationToken cancellationToken = default)
    {
        var isArabic = language.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        var dir = isArabic ? "rtl" : "ltr";

        var subject = isArabic
            ? "ScholarPath — مطلوب معلومات إضافية"
            : "ScholarPath — Additional Information Needed";

        var notesSection = string.IsNullOrWhiteSpace(notes)
            ? ""
            : isArabic
                ? $"<p><strong>ملاحظات:</strong> {notes}</p>"
                : $"<p><strong>Notes:</strong> {notes}</p>";

        var htmlBody = isArabic
            ? $"""
              <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; direction: {dir}; text-align: right;">
                  <h2 style="color: #f59e0b;">مطلوب معلومات إضافية</h2>
                  <p>مرحباً {userName}،</p>
                  <p>يتطلب طلب الترقية الخاص بك معلومات إضافية. يرجى تحديث طلبك.</p>
                  {notesSection}
                  <hr style="border: none; border-top: 1px solid #e5e7eb;" />
                  <p style="color: #9ca3af; font-size: 12px;">فريق ScholarPath</p>
              </div>
              """
            : $"""
              <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; direction: {dir};">
                  <h2 style="color: #f59e0b;">Additional Information Needed</h2>
                  <p>Hello {userName},</p>
                  <p>Your upgrade request requires additional information. Please update your request.</p>
                  {notesSection}
                  <hr style="border: none; border-top: 1px solid #e5e7eb;" />
                  <p style="color: #9ca3af; font-size: 12px;">ScholarPath Team</p>
              </div>
              """;

        await SendEmailAsync(to, subject, htmlBody, cancellationToken);
    }

    private async Task SendViaSmtpAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
            message.To.Add(new MailAddress(to));
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort);
            client.EnableSsl = _emailSettings.EnableSsl;

            if (!string.IsNullOrWhiteSpace(_emailSettings.SmtpUser))
            {
                client.Credentials = new NetworkCredential(_emailSettings.SmtpUser, _emailSettings.SmtpPassword);
            }

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("[EMAIL:SMTP] Sent email to {To}, Subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL:SMTP] Failed to send email to {To}, Subject: {Subject}", to, subject);
            throw;
        }
    }

    private async Task SendViaSendGridAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        try
        {
            var client = new SendGridClient(_emailSettings.SendGridApiKey);
            var from = new EmailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
            var toAddress = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, null, htmlBody);

            var response = await client.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[EMAIL:SendGrid] Sent email to {To}, Subject: {Subject}", to, subject);
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "[EMAIL:SendGrid] Failed to send email to {To}, Subject: {Subject}, StatusCode: {StatusCode}, Response: {Response}",
                    to, subject, response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL:SendGrid] Failed to send email to {To}, Subject: {Subject}", to, subject);
            throw;
        }
    }
}
