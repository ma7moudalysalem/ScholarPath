using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Real SMTP email delivery via MailKit (Task 5B). Failure-safe: a delivery error is
/// logged and swallowed so a missing/unreachable SMTP server never breaks the caller
/// (email is a best-effort side channel — the InApp notification still persists).
/// </summary>
public sealed class MailKitEmailService(
    IOptions<EmailOptions> options,
    ILogger<MailKitEmailService> logger) : IEmailService
{
    private readonly EmailOptions _opts = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        try
        {
            using var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(_opts.FromName, _opts.From));
            mime.To.Add(MailboxAddress.Parse(message.To));
            mime.Subject = message.Subject;
            if (!string.IsNullOrEmpty(message.ReplyTo))
                mime.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));

            mime.Body = new BodyBuilder
            {
                HtmlBody = message.HtmlBody,
                TextBody = message.TextBody ?? string.Empty,
            }.ToMessageBody();

            var smtp = _opts.MailKit;
            using var client = new SmtpClient { Timeout = 10_000 };
            try
            {
                await client.ConnectAsync(
                    smtp.Host, smtp.Port,
                    smtp.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable,
                    ct);
                if (!string.IsNullOrEmpty(smtp.Username))
                    await client.AuthenticateAsync(smtp.Username, smtp.Password, ct);
                await client.SendAsync(mime, ct);
            }
            finally
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(true, ct);
            }

            logger.LogInformation("[email] sent to {To} subject={Subject}", message.To, message.Subject);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[email] delivery to {To} failed.", message.To);
        }
    }
}
