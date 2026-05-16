using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    IApplicationDbContext db,
    IEmailService emailService,
    IDateTimeService clock,
    IOptions<AppOptions> appOptions)
    : IRequestHandler<ForgotPasswordCommand>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        // Always succeed silently — never reveal whether the email is registered.
        if (user is null)
            return;

        var now = clock.UtcNow;

        // Invalidate the user's earlier pending reset tokens (one live link at a time).
        var pending = await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null && t.ExpiresAt > now)
            .ToListAsync(ct);
        foreach (var stale in pending)
            stale.UsedAt = now;

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Hash(rawToken),
            ExpiresAt = now + TokenLifetime,
        });
        await db.SaveChangesAsync(ct);

        var link = $"{appOptions.Value.ClientUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        await emailService.SendAsync(new EmailMessage(
            To: user.Email!,
            Subject: "Reset your ScholarPath password",
            HtmlBody:
                $"<p>Hi {user.FirstName},</p>" +
                "<p>We received a request to reset your ScholarPath password. " +
                "The link below is valid for one hour and can be used once:</p>" +
                $"<p><a href=\"{link}\">Reset my password</a></p>" +
                "<p>If you didn't request this, you can safely ignore this email.</p>",
            TextBody: $"Reset your ScholarPath password (valid 1 hour, single use): {link}"),
            ct);
    }

    private static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
