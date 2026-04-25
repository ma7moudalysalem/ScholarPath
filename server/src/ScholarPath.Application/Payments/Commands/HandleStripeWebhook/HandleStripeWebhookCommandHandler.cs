using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Payments.Commands.HandleStripeWebhook;

public sealed class HandleStripeWebhookCommandHandler : IRequestHandler<HandleStripeWebhookCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IStripeService _stripeService;
    private readonly IOptions<StripeSettings> _stripeOptions;
    private readonly ILogger<HandleStripeWebhookCommandHandler> _logger;

    public HandleStripeWebhookCommandHandler(
        IApplicationDbContext context,
        IStripeService stripeService,
        IOptions<StripeSettings> stripeOptions,
        ILogger<HandleStripeWebhookCommandHandler> logger)
    {
        _context = context;
        _stripeService = stripeService;
        _stripeOptions = stripeOptions;
        _logger = logger;
    }

    public async Task Handle(HandleStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        var webhookSecret =
            _stripeOptions.Value.WebhookSecret ??
            _stripeOptions.Value.WebhookSigningSecret ??
            string.Empty;

        var parsed = _stripeService.ParseWebhook(
            request.Payload,
            request.SignatureHeader ?? string.Empty,
            webhookSecret);

        var webhookEvent = await _context.StripeWebhookEvents
            .FirstOrDefaultAsync(e => e.StripeEventId == parsed.EventId, cancellationToken);

        if (webhookEvent is not null && webhookEvent.IsProcessed)
        {
            _logger.LogInformation(
                "Stripe webhook {StripeEventId} already processed. Skipping.",
                parsed.EventId);
            return;
        }

        var nowUtc = DateTimeOffset.UtcNow;

        if (webhookEvent is null)
        {
            webhookEvent = new StripeWebhookEvent
            {
                StripeEventId = parsed.EventId,
                EventType = parsed.EventType,
                RawPayload = request.Payload,
                ReceivedAt = nowUtc,
                IsProcessed = false,
                ProcessedAt = null,
                ProcessingError = null,
                ProcessingAttempts = 1
            };

            _context.StripeWebhookEvents.Add(webhookEvent);
        }
        else
        {
            webhookEvent.EventType = parsed.EventType;
            webhookEvent.RawPayload = request.Payload;
            webhookEvent.ProcessingAttempts += 1;
            webhookEvent.ProcessingError = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            using var document = JsonDocument.Parse(parsed.DataJson);
            var stripeObject = ExtractStripeObject(document.RootElement);

            switch (parsed.EventType)
            {
                case "payment_intent.amount_capturable_updated":
                    await HandleAmountCapturableUpdatedAsync(stripeObject, cancellationToken);
                    break;

                case "payment_intent.succeeded":
                    await HandlePaymentIntentSucceededAsync(stripeObject, cancellationToken);
                    break;

                case "payment_intent.payment_failed":
                    await HandlePaymentIntentFailedAsync(stripeObject, cancellationToken);
                    break;

                case "payment_intent.canceled":
                    await HandlePaymentIntentCanceledAsync(stripeObject, cancellationToken);
                    break;

                case "charge.refunded":
                    await HandleChargeRefundedAsync(stripeObject, cancellationToken);
                    break;

                case "payment_intent.requires_action":
                    _logger.LogWarning(
                        "Stripe webhook event type {EventType} was received but is not handled yet.",
                        parsed.EventType);
                    break;

                case "charge.dispute.created":
                    _logger.LogWarning(
                        "Stripe webhook event type {EventType} was received but is not handled yet.",
                        parsed.EventType);
                    break;

                default:
                    _logger.LogInformation(
                        "Stripe webhook event type {EventType} is not handled explicitly. Recorded only.",
                        parsed.EventType);
                    break;
            }

            webhookEvent.IsProcessed = true;
            webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;
            webhookEvent.ProcessingError = null;

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            webhookEvent.IsProcessed = false;
            webhookEvent.ProcessingError = ex.Message;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogError(
                ex,
                "Failed to process Stripe webhook {StripeEventId} ({EventType}).",
                parsed.EventId,
                parsed.EventType);

            throw;
        }
    }

    private async Task HandleAmountCapturableUpdatedAsync(JsonElement stripeObject, CancellationToken cancellationToken)
    {
        var paymentIntentId = GetString(stripeObject, "id", "payment_intent");
        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            _logger.LogWarning("Webhook payment_intent.amount_capturable_updated has no payment intent id.");
            return;
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);

        if (payment is null)
        {
            _logger.LogWarning("No Payment row found for Stripe payment intent {PaymentIntentId}.", paymentIntentId);
            return;
        }

        payment.Status = PaymentStatus.Held;
        payment.HeldAt ??= DateTimeOffset.UtcNow;
        payment.FailureReason = null;
    }

    private async Task HandlePaymentIntentSucceededAsync(JsonElement stripeObject, CancellationToken cancellationToken)
    {
        var paymentIntentId = GetString(stripeObject, "id", "payment_intent");
        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            _logger.LogWarning("Webhook payment_intent.succeeded has no payment intent id.");
            return;
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);

        if (payment is null)
        {
            _logger.LogWarning("No Payment row found for Stripe payment intent {PaymentIntentId}.", paymentIntentId);
            return;
        }

        payment.Status = PaymentStatus.Captured;
        payment.CapturedAt ??= DateTimeOffset.UtcNow;
        payment.FailureReason = null;

        var chargeId = GetString(stripeObject, "latest_charge", "charge");
        if (!string.IsNullOrWhiteSpace(chargeId))
        {
            payment.StripeChargeId = chargeId;
        }

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.StripePaymentIntentId == paymentIntentId, cancellationToken);

        if (booking is { Status: BookingStatus.Requested })
        {
            booking.Status = BookingStatus.Confirmed;
            booking.ConfirmedAt ??= DateTimeOffset.UtcNow;
        }
    }

    private async Task HandlePaymentIntentFailedAsync(JsonElement stripeObject, CancellationToken cancellationToken)
    {
        var paymentIntentId = GetString(stripeObject, "id", "payment_intent");
        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            _logger.LogWarning("Webhook payment_intent.payment_failed has no payment intent id.");
            return;
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);

        if (payment is null)
        {
            _logger.LogWarning("No Payment row found for failed Stripe payment intent {PaymentIntentId}.", paymentIntentId);
            return;
        }

        payment.Status = PaymentStatus.Failed;
        payment.FailureReason =
            GetNestedString(stripeObject, "last_payment_error", "message") ??
            GetNestedString(stripeObject, "error", "message") ??
            GetString(stripeObject, "status") ??
            "payment_intent.payment_failed";

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.StripePaymentIntentId == paymentIntentId, cancellationToken);

        if (booking is not null &&
            booking.Status != BookingStatus.Cancelled &&
            booking.Status != BookingStatus.Completed &&
            booking.Status != BookingStatus.NoShowStudent &&
            booking.Status != BookingStatus.NoShowConsultant)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt ??= DateTimeOffset.UtcNow;
        }
    }

    private async Task HandlePaymentIntentCanceledAsync(JsonElement stripeObject, CancellationToken cancellationToken)
    {
        var paymentIntentId = GetString(stripeObject, "id", "payment_intent");
        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            _logger.LogWarning("Webhook payment_intent.canceled has no payment intent id.");
            return;
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);

        if (payment is null)
        {
            _logger.LogWarning("No Payment row found for Stripe payment intent {PaymentIntentId}.", paymentIntentId);
            return;
        }

        payment.Status = PaymentStatus.Cancelled;
        payment.FailureReason =
            GetString(stripeObject, "cancellation_reason", "reason", "status")
            ?? "payment_intent.canceled";
    }

    private async Task HandleChargeRefundedAsync(JsonElement stripeObject, CancellationToken cancellationToken)
    {
        var chargeId = GetString(stripeObject, "id", "charge");
        var paymentIntentId = GetString(stripeObject, "payment_intent");
        var amountRefunded = GetLong(stripeObject, "amount_refunded", "amount");

        Payment? payment = null;

        if (!string.IsNullOrWhiteSpace(chargeId))
        {
            payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.StripeChargeId == chargeId, cancellationToken);
        }

        if (payment is null && !string.IsNullOrWhiteSpace(paymentIntentId))
        {
            payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);
        }

        if (payment is null)
        {
            _logger.LogWarning(
                "No Payment row found for refunded charge {ChargeId} / payment intent {PaymentIntentId}.",
                chargeId,
                paymentIntentId);
            return;
        }

        var refundedAmount = amountRefunded ?? payment.AmountCents;

        payment.RefundedAmountCents = refundedAmount;
        payment.RefundedAt = DateTimeOffset.UtcNow;
        payment.RefundReason =
            GetString(stripeObject, "reason", "refund_reason", "status")
            ?? "charge.refunded";

        payment.Status = refundedAmount >= payment.AmountCents
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
    }

    private static JsonElement ExtractStripeObject(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("object", out var nestedObject))
        {
            return nestedObject;
        }

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("object", out var directObject) &&
            directObject.ValueKind == JsonValueKind.Object)
        {
            return directObject;
        }

        return root;
    }

    private static string? GetString(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            if (value.ValueKind == JsonValueKind.Number)
            {
                return value.GetRawText();
            }
        }

        return null;
    }

    private static string? GetNestedString(JsonElement element, string objectPropertyName, params string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!element.TryGetProperty(objectPropertyName, out var nested) ||
            nested.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return GetString(nested, propertyNames);
    }

    private static long? GetLong(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String &&
                long.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}

public sealed class StripeSettings
{
    public string? WebhookSecret { get; set; }
    public string? WebhookSigningSecret { get; set; }
}
