using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Payments.Commands.CreateConnectAccount;

// ─── Result ───────────────────────────────────────────────────────────────────

public sealed record CreateConnectAccountResult(
    string ConnectAccountId,
    StripeConnectStatus Status,
    string OnboardingUrl);

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Creates (or reuses) the authenticated payee's Stripe Connect account and returns
/// a fresh onboarding link. Payees are consultants and companies (PB-013 AC#4).
/// The Connect account id is resolved server-side — never supplied by the client.
/// </summary>
[Auditable(AuditAction.Create, "StripeConnectAccount",
    SummaryTemplate = "Stripe Connect onboarding initiated")]
public sealed record CreateConnectAccountCommand(
    string ReturnUrl,
    string RefreshUrl) : IRequest<CreateConnectAccountResult>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class CreateConnectAccountCommandValidator
    : AbstractValidator<CreateConnectAccountCommand>
{
    public CreateConnectAccountCommandValidator()
    {
        RuleFor(x => x.ReturnUrl)
            .NotEmpty().WithMessage("ReturnUrl is required.")
            .Must(BeAbsoluteUrl).WithMessage("ReturnUrl must be an absolute http(s) URL.");

        RuleFor(x => x.RefreshUrl)
            .NotEmpty().WithMessage("RefreshUrl is required.")
            .Must(BeAbsoluteUrl).WithMessage("RefreshUrl must be an absolute http(s) URL.");
    }

    private static bool BeAbsoluteUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) &&
        (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class CreateConnectAccountCommandHandler(
    IApplicationDbContext db,
    IStripeService stripeService,
    ICurrentUserService currentUser,
    ILogger<CreateConnectAccountCommandHandler> logger)
    : IRequestHandler<CreateConnectAccountCommand, CreateConnectAccountResult>
{
    public async Task<CreateConnectAccountResult> Handle(
        CreateConnectAccountCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        // Only payees (consultants and companies) onboard to receive payouts.
        if (!currentUser.IsInRole("Consultant") && !currentUser.IsInRole("ScholarshipProvider"))
            throw new ForbiddenAccessException(
                "Only consultants and companies can onboard for payouts.");

        var user = await db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException(nameof(ApplicationUser), userId);

        var profile = user.Profile
            ?? throw new ConflictException(
                "Complete your profile before onboarding for payouts.");

        // Reuse an existing connected account; otherwise create one and persist its id.
        if (string.IsNullOrEmpty(profile.StripeConnectAccountId))
        {
            var country = user.CountryOfResidence is { Length: 2 } code
                ? code.ToUpperInvariant()
                : "US";

            var account = await stripeService.CreateConnectAccountAsync(
                user.Email ?? string.Empty, country, ct);

            profile.StripeConnectAccountId = account.Id;
            profile.StripeConnectStatus = StripeConnectStatus.Pending;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            logger.LogInformation(
                "Created Stripe Connect account {AccountId} for user {UserId}.",
                account.Id, userId);
        }

        var onboardingUrl = await stripeService.CreateConnectOnboardingLinkAsync(
            profile.StripeConnectAccountId!, request.RefreshUrl, request.ReturnUrl, ct);

        return new CreateConnectAccountResult(
            profile.StripeConnectAccountId!, profile.StripeConnectStatus, onboardingUrl);
    }
}
