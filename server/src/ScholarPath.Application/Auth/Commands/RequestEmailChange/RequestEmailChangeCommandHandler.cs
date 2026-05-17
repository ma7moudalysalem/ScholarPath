using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.RequestEmailChange;

public sealed class RequestEmailChangeCommandHandler(
    IApplicationDbContext db,
    IEmailChangeService emailChangeService,
    IEmailService emailService,
    ICurrentUserService currentUser,
    IOptions<AppOptions> appOptions)
    : IRequestHandler<RequestEmailChangeCommand>
{
    public async Task Handle(RequestEmailChangeCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var newEmail = request.NewEmail.Trim();
        var normalizedNew = newEmail.ToUpperInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId);

        if (string.Equals(user.NormalizedEmail, normalizedNew, StringComparison.Ordinal))
            throw new ConflictException("The new email address matches your current email.");

        // Uniqueness — no other account may already own the target address.
        var taken = await db.Users
            .AnyAsync(u => u.Id != userId && u.NormalizedEmail == normalizedNew, ct);
        if (taken)
            throw new ConflictException("That email address is already in use.");

        var token = await emailChangeService
            .GenerateChangeEmailTokenAsync(userId, newEmail, ct);

        var link =
            $"{appOptions.Value.ClientUrl.TrimEnd('/')}/confirm-email-change" +
            $"?token={Uri.EscapeDataString(token)}" +
            $"&email={Uri.EscapeDataString(newEmail)}";

        await emailService.SendAsync(new EmailMessage(
            To: newEmail,
            Subject: "Confirm your new ScholarPath email address",
            HtmlBody:
                $"<p>Hi {user.FirstName},</p>" +
                "<p>We received a request to change the email address on your " +
                "ScholarPath account. Confirm the change by following the link below:</p>" +
                $"<p><a href=\"{link}\">Confirm my new email address</a></p>" +
                "<p>If you didn't request this, you can safely ignore this email — " +
                "your account email will not change.</p>",
            TextBody: $"Confirm your new ScholarPath email address: {link}"),
            ct);
    }
}
