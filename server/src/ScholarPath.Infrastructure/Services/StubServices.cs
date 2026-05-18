using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Default fallback implementations, registered by <c>AddInfrastructureServices</c>
/// only when the corresponding real provider is not configured (no SMTP host,
/// no OAuth credentials, no Stripe key, no ClamAV daemon). <c>StubPasswordHasher</c>
/// is always used — it wraps the ASP.NET Identity password hasher.
/// </summary>

public sealed class StubEmailService(ILogger<StubEmailService> logger) : IEmailService
{
    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        logger.LogInformation("[stub-email] to={To} subject={Subject}", message.To, message.Subject);
        return Task.CompletedTask;
    }
}

public sealed class StubSsoService : ISsoService
{
    public Task<SsoUserInfo> ExchangeGoogleCodeAsync(string code, string redirectUri, CancellationToken ct) =>
        Task.FromResult(new SsoUserInfo("stub+google@scholarpath.local", "Stub", "Google", null, "Google", Guid.NewGuid().ToString("N")));

    public Task<SsoUserInfo> ExchangeMicrosoftCodeAsync(string code, string redirectUri, CancellationToken ct) =>
        Task.FromResult(new SsoUserInfo("stub+ms@scholarpath.local", "Stub", "Microsoft", null, "Microsoft", Guid.NewGuid().ToString("N")));

    public string BuildGoogleAuthorizeUrl(string redirectUri, string state) =>
        $"https://accounts.google.com/o/oauth2/v2/auth?redirect_uri={redirectUri}&state={state}&scope=openid%20email%20profile";

    public string BuildMicrosoftAuthorizeUrl(string redirectUri, string state) =>
        $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?redirect_uri={redirectUri}&state={state}&scope=openid%20email%20profile";
}

public sealed class StubPasswordHasher : IPasswordHasher
{
    // Wraps Identity's built-in PasswordHasher for v2 — but kept behind interface for testability.
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(new object(), password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(new object(), hash, password)
            is Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success
            or Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded;
}

public sealed class StubStripeService(ILogger<StubStripeService> logger) : IStripeService
{
    public Task<StripePaymentIntentResult> CreatePaymentIntentAsync(long amountCents, string currency, string captureMethod, IDictionary<string, string>? metadata, string idempotencyKey, CancellationToken ct)
    {
        var id = $"pi_stub_{Guid.NewGuid():N}";
        logger.LogInformation("[stub-stripe] create intent {Id} amount={Amount}{Currency}", id, amountCents, currency);
        return Task.FromResult(new StripePaymentIntentResult(id, "requires_capture", $"cs_stub_{id}", null));
    }

    public Task<StripePaymentIntentResult> CapturePaymentIntentAsync(string paymentIntentId, long? amountToCaptureCents, string idempotencyKey, CancellationToken ct)
    {
        logger.LogInformation("[stub-stripe] capture {Id}", paymentIntentId);
        return Task.FromResult(new StripePaymentIntentResult(paymentIntentId, "succeeded", null, $"ch_stub_{Guid.NewGuid():N}"));
    }

    public Task<StripePaymentIntentResult> CancelPaymentIntentAsync(string paymentIntentId, string? cancellationReason, string idempotencyKey, CancellationToken ct)
    {
        logger.LogInformation("[stub-stripe] cancel {Id}", paymentIntentId);
        return Task.FromResult(new StripePaymentIntentResult(paymentIntentId, "canceled", null, null));
    }

    public Task<StripeRefundResult> RefundPaymentAsync(string paymentIntentId, long? amountCents, string? reason, string idempotencyKey, CancellationToken ct)
    {
        logger.LogInformation("[stub-stripe] refund {Id}", paymentIntentId);
        return Task.FromResult(new StripeRefundResult($"re_stub_{Guid.NewGuid():N}", "succeeded", amountCents ?? 0));
    }

    public Task<StripeConnectAccountResult> CreateConnectAccountAsync(string email, string country, CancellationToken ct)
    {
        var id = $"acct_stub_{Guid.NewGuid():N}";
        logger.LogInformation("[stub-stripe] create connect account {Id}", id);
        return Task.FromResult(new StripeConnectAccountResult(id, "pending_verification"));
    }

    public Task<string> CreateConnectOnboardingLinkAsync(string connectAccountId, string refreshUrl, string returnUrl, CancellationToken ct) =>
        Task.FromResult($"https://stripe-stub/onboarding/{connectAccountId}");

    public Task<StripePayoutResult> CreatePayoutAsync(string destinationConnectAccountId, long amountCents, string currency, string idempotencyKey, CancellationToken ct)
    {
        logger.LogInformation("[stub-stripe] payout {Account} {Amount}", destinationConnectAccountId, amountCents);
        return Task.FromResult(new StripePayoutResult($"po_stub_{Guid.NewGuid():N}", "in_transit"));
    }

   public StripeWebhookParseResult ParseWebhook(string payload, string signatureHeader, string webhookSecret) =>
        new("evt_stub", "payment_intent.succeeded", "pi_stub", null, null, payload);

}

/// <summary>
/// No-op <see cref="IFileScanService"/> that always reports files as clean.
/// Registered when <c>FileScanning:Enabled</c> is false — dev and test
/// environments where no ClamAV daemon is running. Production must enable the
/// real <c>ClamAvFileScanService</c>.
/// </summary>
public sealed class NoOpFileScanService(ILogger<NoOpFileScanService> logger) : IFileScanService
{
    public Task<FileScanResult> ScanAsync(Stream content, string fileName, CancellationToken ct)
    {
        logger.LogDebug("[noop-scan] {FileName} not scanned (file scanning disabled).", fileName);
        return Task.FromResult(new FileScanResult(FileScanVerdict.Clean, null));
    }
}
