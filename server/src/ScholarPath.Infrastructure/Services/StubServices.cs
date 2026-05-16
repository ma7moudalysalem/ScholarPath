using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Stub implementations used as defaults in dev. Swap to real providers via DI registration.
/// Teammates replace these with production logic per their module specs.
/// </summary>

public sealed class StubEmailService(ILogger<StubEmailService> logger) : IEmailService
{
    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        logger.LogInformation("[stub-email] to={To} subject={Subject}", message.To, message.Subject);
        return Task.CompletedTask;
    }
}

public sealed class StubBlobStorageService(ILogger<StubBlobStorageService> logger) : IBlobStorageService
{
    public Task<string> UploadAsync(Stream content, string fileName, string contentType, string container, CancellationToken ct)
    {
        var fakeUrl = $"https://scholarpath-stub.blob/{container}/{Guid.NewGuid()}/{fileName}";
        logger.LogInformation("[stub-blob] upload {File} -> {Url}", fileName, fakeUrl);
        return Task.FromResult(fakeUrl);
    }

    public Task DeleteAsync(string blobUrl, CancellationToken ct)
    {
        logger.LogInformation("[stub-blob] delete {Url}", blobUrl);
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string blobUrl, CancellationToken ct)
    {
        logger.LogInformation("[stub-blob] download {Url}", blobUrl);
        return Task.FromResult<Stream>(new MemoryStream());
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

public sealed class StubAiService : IAiService
{
    private const string Disclaimer = "AI-generated guidance. Verify with official sources before acting.";

    public Task<AiRecommendationResult> GenerateRecommendationsAsync(Guid userId, int topN, CancellationToken ct) =>
        Task.FromResult(new AiRecommendationResult(Array.Empty<AiRecommendationItem>(), Disclaimer, 0, 0));

    public Task<AiEligibilityResult> CheckEligibilityAsync(Guid userId, Guid scholarshipId, CancellationToken ct) =>
        Task.FromResult(new AiEligibilityResult(
            Array.Empty<AiEligibilityCriterion>(),
            "No eligibility data available in stub mode.",
            Disclaimer));

    public Task<AiChatResponse> AskAsync(Guid userId, string sessionId, string message, CancellationToken ct) =>
        Task.FromResult(new AiChatResponse(
            $"(stub) I heard: {message[..Math.Min(message.Length, 120)]}",
            Disclaimer,
            PromptTokens: 0,
            CompletionTokens: 0,
            EstimatedCostUsd: 0m));
}

public sealed class StubNotificationDispatcher(ILogger<StubNotificationDispatcher> logger) : INotificationDispatcher
{
    public Task DispatchAsync(Guid recipientUserId, NotificationType type, NotificationContent content, string? deepLink, string? idempotencyKey, CancellationToken ct)
    {
        logger.LogInformation("[stub-notif] user={UserId} type={Type}", recipientUserId, type);
        return Task.CompletedTask;
    }

    public Task DispatchBroadcastAsync(IReadOnlyCollection<Guid> recipientUserIds, NotificationType type, NotificationContent content, CancellationToken ct)
    {
        logger.LogInformation("[stub-notif] broadcast type={Type} recipients={Count}", type, recipientUserIds.Count);
        return Task.CompletedTask;
    }
}

public sealed class StubAuditService(ILogger<StubAuditService> logger) : IAuditService
{
    public Task WriteAsync(AuditAction action, string targetType, Guid? targetId, string? beforeJson, string? afterJson, string? summary, CancellationToken ct)
    {
        logger.LogInformation("[stub-audit] {Action} {TargetType} {TargetId}", action, targetType, targetId);
        return Task.CompletedTask;
    }
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
