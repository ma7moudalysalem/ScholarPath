using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Auth;

/// <summary>
/// Builds and sends the account email-verification message (FR-215). Shared by
/// registration and the resend-verification flow so the link and copy stay
/// consistent. Uses Identity's built-in confirmation token.
/// </summary>
public static class EmailVerificationSender
{
    /// <summary>
    /// Generates an Identity confirmation token for <paramref name="user"/> and
    /// emails a verification link. Best-effort: a missing token (e.g. user gone)
    /// is skipped silently.
    /// </summary>
    public static async Task SendAsync(
        ApplicationUser user,
        IEmailVerificationService verification,
        IEmailService emailService,
        string clientUrl,
        CancellationToken ct)
    {
        var token = await verification.GenerateConfirmationTokenAsync(user.Id, ct).ConfigureAwait(false);
        if (token is null)
            return;

        var link = $"{clientUrl.TrimEnd('/')}/verify-email" +
                   $"?userId={Uri.EscapeDataString(user.Id.ToString())}" +
                   $"&token={Uri.EscapeDataString(token)}";

        await emailService.SendAsync(new EmailMessage(
            To: user.Email!,
            Subject: "Verify your ScholarPath email address",
            HtmlBody:
                $"<p>Hi {user.FirstName},</p>" +
                "<p>Thanks for creating a ScholarPath account. " +
                "Please confirm this email address to finish setting up your account:</p>" +
                $"<p><a href=\"{link}\">Verify my email</a></p>" +
                "<p>If you didn't create this account, you can safely ignore this email.</p>",
            TextBody: $"Verify your ScholarPath email address: {link}"),
            ct).ConfigureAwait(false);
    }
}
