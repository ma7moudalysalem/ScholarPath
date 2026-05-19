
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
            CancellationReason = cancellationReason,
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
            throw;
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
            Reason = reason,
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
            throw;
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
                    amountCents = ch.Amount;
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
