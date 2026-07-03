
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Exceptions;
using ScholarPath.Infrastructure.Settings;
using Stripe;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Real Stripe.net implementation of IStripeService.
/// Registered in DI when StripeOptions.SecretKey is present (non-stub).
/// </summary>
public sealed class StripeService(
    IOptions<StripeOptions> options,
    ILogger<StripeService> logger) : IStripeService
{
    private readonly StripeOptions _opts = options.Value;

    // Stripe accepts only a fixed vocabulary for these optional fields; sending
    // an unrecognised value is a hard 400 from the API. Booking handlers keep
    // their own domain reason on the Payment row, so an unmapped value is just
    // omitted here rather than failing the whole cancellation/refund.
    private static readonly HashSet<string> ValidCancellationReasons =
        new(StringComparer.OrdinalIgnoreCase)
        { "duplicate", "fraudulent", "requested_by_customer", "abandoned" };

    private static readonly HashSet<string> ValidRefundReasons =
        new(StringComparer.OrdinalIgnoreCase)
        { "duplicate", "fraudulent", "requested_by_customer" };

    // MON-03: a Stripe `charge.refunded` payload should never report a negative
    // cumulative AmountRefunded, but a malformed/replayed event must not push a
    // negative value downstream (refund notices, review-payment status, payout
    // split). Clamp to >= 0 at the source and log the anomaly. The handler's
    // DATA-02 clamp is the ledger backstop; this stops the bad value one layer up.
    private static long NonNegativeRefund(long amountRefunded, ILogger logger, string? chargeId)
    {
        if (amountRefunded < 0)
        {
            logger.LogWarning(
                "[stripe-webhook] charge.refunded for {ChargeId} reported a negative AmountRefunded={Amount}; clamping to 0.",
                chargeId, amountRefunded);
            return 0;
        }

        return amountRefunded;
    }

    private static string? StripeReasonOrNull(string? reason, HashSet<string> allowed)
        => reason is not null && allowed.TryGetValue(reason, out var canonical)
            ? canonical
            : null;

    // ── Create PaymentIntent ──────────────────────────────────────────────────

    public async Task<StripePaymentIntentResult> CreatePaymentIntentAsync(
        long amountCents,
        string currency,
        string captureMethod,
        IDictionary<string, string>? metadata,
        string idempotencyKey,
        CancellationToken ct)
    {
        var service = new PaymentIntentService();

        var createOptions = new PaymentIntentCreateOptions
        {
            Amount = amountCents,
            Currency = currency,
            CaptureMethod = captureMethod,
            Metadata = metadata?.ToDictionary(k => k.Key, v => v.Value),
            // Restrict to card. Without this Stripe inherits whatever wallets are
            // toggled on in the dashboard (Amazon Pay, Cash App Pay, …) and the
            // PaymentElement renders them even though we never set them up — which
            // confused users and would fail at confirm time. Card only keeps the
            // checkout to the one method we actually support.
            PaymentMethodTypes = ["card"],
        };

        var requestOptions = new RequestOptions
        {
            ApiKey = _opts.SecretKey,
            IdempotencyKey = idempotencyKey,
        };

        try
        {
            var intent = await service.CreateAsync(createOptions, requestOptions, ct);

            logger.LogInformation(
                "[stripe] Created PaymentIntent {Id} status={Status} amount={Amount}{Currency}",
                intent.Id, intent.Status, amountCents, currency);

            return new StripePaymentIntentResult(
                intent.Id,
                intent.Status,
                intent.ClientSecret,
                intent.LatestChargeId);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex,
                "[stripe] Failed to create PaymentIntent. amount={Amount} key={Key}",
                amountCents, idempotencyKey);
            throw;
        }
    }

    // ── Capture PaymentIntent ─────────────────────────────────────────────────

    public async Task<StripePaymentIntentResult> CapturePaymentIntentAsync(
        string paymentIntentId,
        long? amountToCaptureCents,
        string idempotencyKey,
        CancellationToken ct)
    {
        var service = new PaymentIntentService();

        var captureOptions = new PaymentIntentCaptureOptions
        {
            AmountToCapture = amountToCaptureCents,
        };

        var requestOptions = new RequestOptions
        {
            ApiKey = _opts.SecretKey,
            IdempotencyKey = idempotencyKey,
        };

        try
        {
            var intent = await service.CaptureAsync(
                paymentIntentId, captureOptions, requestOptions, ct);

            logger.LogInformation(
                "[stripe] Captured PaymentIntent {Id} status={Status}",
                intent.Id, intent.Status);

            return new StripePaymentIntentResult(
                intent.Id,
                intent.Status,
                intent.ClientSecret,
                intent.LatestChargeId);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex,
                "[stripe] Failed to capture PaymentIntent {Id}",
                paymentIntentId);
            // A capture failure is almost always a payment-state problem — the
            // student never authorized the card, so the intent is not in
            // `requires_capture`. Surface it as a clean 422 ("Booking rule
            // violated"), not an opaque 500.
            throw new BookingDomainException(
                "The session fee could not be captured. The student may not have "
                + "completed the card authorization for this booking yet — ask them "
                + "to finish payment, then try accepting again.",
                ex);
        }
    }

    // ── Cancel PaymentIntent ──────────────────────────────────────────────────

    public async Task<StripePaymentIntentResult> CancelPaymentIntentAsync(
        string paymentIntentId,
        string? cancellationReason,
        string idempotencyKey,
        CancellationToken ct)
    {
        var service = new PaymentIntentService();

        var cancelOptions = new PaymentIntentCancelOptions
        {
            CancellationReason = StripeReasonOrNull(cancellationReason, ValidCancellationReasons),
        };

        var requestOptions = new RequestOptions
        {
            ApiKey = _opts.SecretKey,
            IdempotencyKey = idempotencyKey,
        };

        try
        {
            var intent = await service.CancelAsync(
                paymentIntentId, cancelOptions, requestOptions, ct);

            logger.LogInformation(
                "[stripe] Cancelled PaymentIntent {Id} status={Status}",
                intent.Id, intent.Status);

            return new StripePaymentIntentResult(
                intent.Id,
                intent.Status,
                intent.ClientSecret,
                null);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex,
                "[stripe] Failed to cancel PaymentIntent {Id}",
                paymentIntentId);
            throw new BookingDomainException(
                "The booking's payment hold could not be released. Please try again.",
                ex);
        }
    }

    // ── Refund ────────────────────────────────────────────────────────────────

    public async Task<StripeRefundResult> RefundPaymentAsync(
        string paymentIntentId,
        long? amountCents,
        string? reason,
        string idempotencyKey,
        CancellationToken ct)
    {
        var service = new RefundService();

        var refundOptions = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId,
            Amount = amountCents,
            Reason = StripeReasonOrNull(reason, ValidRefundReasons),
        };

        var requestOptions = new RequestOptions
        {
            ApiKey = _opts.SecretKey,
            IdempotencyKey = idempotencyKey,
        };

        try
        {
            var refund = await service.CreateAsync(
                refundOptions, requestOptions, ct);

            logger.LogInformation(
                "[stripe] Refund {RefundId} status={Status} amount={Amount}",
                refund.Id, refund.Status, refund.Amount);

            return new StripeRefundResult(
                refund.Id,
                refund.Status,
                refund.Amount);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex,
                "[stripe] Failed to refund PaymentIntent {Id}",
                paymentIntentId);
            throw new BookingDomainException(
                "The refund could not be processed. Please try again.",
                ex);
        }
    }

    // ── Connect Account ───────────────────────────────────────────────────────

    public async Task<StripeConnectAccountResult> CreateConnectAccountAsync(
        string email,
        string country,
        CancellationToken ct)
    {
        var service = new AccountService();

        var createOptions = new AccountCreateOptions
        {
            Type = "express",
            Email = email,
            Country = country,
        };

        var requestOptions = new RequestOptions
        {
            ApiKey = _opts.SecretKey,
        };

        try
        {
            var account = await service.CreateAsync(
                createOptions, requestOptions, ct);

            logger.LogInformation(
                "[stripe] Created Connect account {Id} status={Status}",
                account.Id, account.Requirements?.CurrentlyDue?.Any() == true
                    ? "pending_verification"
                    : "active");

            return new StripeConnectAccountResult(
                account.Id,
                "pending_verification");
        }
        catch (StripeException ex)
        {
            logger.LogError(ex,
                "[stripe] Failed to create Connect account for {Email}", email);
            throw;
        }
    }

    // ── Connect Onboarding Link ───────────────────────────────────────────────

    public async Task<string> CreateConnectOnboardingLinkAsync(
        string connectAccountId,
        string refreshUrl,
        string returnUrl,
        CancellationToken ct)
    {
        var service = new AccountLinkService();

        var linkOptions = new AccountLinkCreateOptions
        {
            Account = connectAccountId,
            RefreshUrl = refreshUrl,
            ReturnUrl = returnUrl,
            Type = "account_onboarding",
        };

        var requestOptions = new RequestOptions
        {
            ApiKey = _opts.SecretKey,
        };

        try
        {
            var link = await service.CreateAsync(
                linkOptions, requestOptions, ct);

            logger.LogInformation(
                "[stripe] Created onboarding link for account {Id}",
                connectAccountId);

            return link.Url;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex,
                "[stripe] Failed to create onboarding link for account {Id}",
                connectAccountId);
            throw;
        }
    }

    // ── Payout ────────────────────────────────────────────────────────────────

    public async Task<StripePayoutResult> CreatePayoutAsync(
        string destinationConnectAccountId,
        long amountCents,
        string currency,
        string idempotencyKey,
        CancellationToken ct)
    {
        var service = new PayoutService();

        var payoutOptions = new PayoutCreateOptions
        {
            Amount = amountCents,
            Currency = currency,
        };

        var requestOptions = new RequestOptions
        {
            ApiKey = _opts.SecretKey,
            StripeAccount = destinationConnectAccountId,
            IdempotencyKey = idempotencyKey,
        };

        try
        {
            var payout = await service.CreateAsync(
                payoutOptions, requestOptions, ct);

            logger.LogInformation(
                "[stripe] Created payout {Id} status={Status} amount={Amount}",
                payout.Id, payout.Status, amountCents);

            return new StripePayoutResult(payout.Id, payout.Status);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex,
                "[stripe] Failed to create payout for account {Account}",
                destinationConnectAccountId);
            throw;
        }
    }

    // ── Parse Webhook ─────────────────────────────────────────────────────────

    public StripeWebhookParseResult ParseWebhook(
    string payload,
    string signatureHeader,
    string webhookSecret)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signatureHeader,
                webhookSecret,
                throwOnApiVersionMismatch: false);

            // Serialize data.object to JSON using System.Text.Json
            var dataJson = stripeEvent.Data?.Object is not null
                ? System.Text.Json.JsonSerializer.Serialize(stripeEvent.Data.Object)
                : payload;

            logger.LogInformation(
                "[stripe-webhook] Parsed event {EventId} type={EventType}",
                stripeEvent.Id, stripeEvent.Type);

            // Extract the typed sub-object so the webhook handler can reconcile
            // the matching Payment row (idempotency + status updates).
            string? paymentIntentId = null;
            string? chargeId = null;
            long? amountCents = null;
            string? connectAccountId = null;
            bool? connectPayoutsEnabled = null;
            string? payoutId = null;
            string? payoutFailureMessage = null;
            string? disputeReason = null;
            switch (stripeEvent.Data?.Object)
            {
                case PaymentIntent pi:
                    paymentIntentId = pi.Id;
                    amountCents = pi.Amount;
                    break;
                case Charge ch:
                    chargeId = ch.Id;
                    paymentIntentId = ch.PaymentIntentId;
                    // For `charge.refunded` this must be the CUMULATIVE amount
                    // refunded on the charge — using ch.Amount (the gross charge)
                    // made every partial refund record as a full refund and flip
                    // the status to Refunded. ch.AmountRefunded is the only
                    // consumer of this value for a Charge. MON-03: guard against a
                    // malformed/negative value from a spoofed or replayed payload.
                    amountCents = NonNegativeRefund(ch.AmountRefunded, logger, ch.Id);
                    break;
                case Account acct:
                    connectAccountId = acct.Id;
                    connectPayoutsEnabled = acct.PayoutsEnabled;
                    break;
                case Payout po:
                    payoutId = po.Id;
                    payoutFailureMessage = po.FailureMessage;
                    break;
                case Dispute dp:
                    chargeId = dp.ChargeId;
                    paymentIntentId = dp.PaymentIntentId;
                    amountCents = dp.Amount;
                    disputeReason = dp.Reason;
                    break;
            }

            return new StripeWebhookParseResult(
                stripeEvent.Id,
                stripeEvent.Type,
                paymentIntentId,
                chargeId,
                amountCents,
                dataJson,
                connectAccountId,
                connectPayoutsEnabled,
                payoutId,
                payoutFailureMessage,
                disputeReason);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex,
                "[stripe-webhook] Signature verification failed.");
            throw;
        }
    }

}
