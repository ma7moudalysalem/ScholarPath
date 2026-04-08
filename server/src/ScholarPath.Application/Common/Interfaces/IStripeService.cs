namespace ScholarPath.Application.Common.Interfaces;

public interface IStripeService
{
    Task<StripePaymentIntentResult> CreatePaymentIntentAsync(
        long amountCents,
        string currency,
        string captureMethod, // "manual" (hold) or "automatic"
        IDictionary<string, string>? metadata,
        string idempotencyKey,
        CancellationToken ct);

    Task<StripePaymentIntentResult> CapturePaymentIntentAsync(
        string paymentIntentId,
        long? amountToCaptureCents,
        string idempotencyKey,
        CancellationToken ct);

    Task<StripePaymentIntentResult> CancelPaymentIntentAsync(
        string paymentIntentId,
        string? cancellationReason,
        string idempotencyKey,
        CancellationToken ct);

    Task<StripeRefundResult> RefundPaymentAsync(
        string paymentIntentId,
        long? amountCents,
        string? reason,
        string idempotencyKey,
        CancellationToken ct);

    Task<StripeConnectAccountResult> CreateConnectAccountAsync(
        string email,
        string country,
        CancellationToken ct);

    Task<string> CreateConnectOnboardingLinkAsync(
        string connectAccountId,
        string refreshUrl,
        string returnUrl,
        CancellationToken ct);

    Task<StripePayoutResult> CreatePayoutAsync(
        string destinationConnectAccountId,
        long amountCents,
        string currency,
        string idempotencyKey,
        CancellationToken ct);

    /// <summary>
    /// Verifies the webhook signature and returns the parsed event ID + type.
    /// Throws on invalid signature.
    /// </summary>
    StripeWebhookParseResult ParseWebhook(string payload, string signatureHeader, string webhookSecret);
}

public sealed record StripePaymentIntentResult(string Id, string Status, string? ClientSecret, string? LatestChargeId);
public sealed record StripeRefundResult(string Id, string Status, long AmountCents);
public sealed record StripeConnectAccountResult(string Id, string Status);
public sealed record StripePayoutResult(string Id, string Status);
public sealed record StripeWebhookParseResult(string EventId, string EventType, string DataJson);
