using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Payments.Commands.ProcessStripeWebhook;

public sealed record ProcessStripeWebhookCommand(
    string EventId,
    string EventType,
    string? PaymentIntentId,
    string? ChargeId,
    long? AmountCents,
    string DataJson) : IRequest<bool>;

public sealed class ProcessStripeWebhookCommandHandler(
    IApplicationDbContext db,
    ILogger<ProcessStripeWebhookCommandHandler> logger)
    : IRequestHandler<ProcessStripeWebhookCommand, bool>
{
    public async Task<bool> Handle(ProcessStripeWebhookCommand request, CancellationToken ct)
    {
        if (await db.StripeWebhookEvents.AnyAsync(e => e.StripeEventId == request.EventId, ct))
        {
            logger.LogInformation("Webhook {EventId} already processed, skipping.", request.EventId);
            return true;
        }

        var webhookEvent = new StripeWebhookEvent
        {
            StripeEventId = request.EventId,
            EventType = request.EventType,
            RawPayload = request.DataJson,
        };
        db.StripeWebhookEvents.Add(webhookEvent);

        if (string.IsNullOrEmpty(request.PaymentIntentId))
        {
            logger.LogWarning("Webhook {EventId} (type {Type}) has no PaymentIntent ID.", request.EventId, request.EventType);
            webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;
            webhookEvent.IsProcessed = true;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return true;
        }

        var payment = await db.CompanyReviewPayments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == request.PaymentIntentId, ct);

        if (payment == null)
        {
            logger.LogInformation("Webhook {EventId} for unknown payment {IntentId}", request.EventId, request.PaymentIntentId);
            webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;
            webhookEvent.IsProcessed = true;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return true;
        }

        if (request.EventType is "payment_intent.succeeded" or "payment_intent.amount_capturable_updated")
        {
            if (payment.Status == PaymentStatus.Pending)
            {
                payment.Status = PaymentStatus.Held;
                logger.LogInformation("Payment {IntentId} updated to Held via webhook", request.PaymentIntentId);
            }
        }
        else if (request.EventType == "charge.refunded")
        {
            payment.Status = PaymentStatus.Refunded;
            logger.LogInformation("Payment {IntentId} updated to Refunded via webhook", request.PaymentIntentId);
        }

        webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;
        webhookEvent.IsProcessed = true;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
