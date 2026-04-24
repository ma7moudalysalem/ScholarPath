using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Payments.Commands.ProcessStripeWebhook;

public sealed record ProcessStripeWebhookCommand(
    string EventId,
    string EventType,
    string DataJson) : IRequest<bool>;

public sealed class ProcessStripeWebhookCommandHandler(
    IApplicationDbContext db,
    ILogger<ProcessStripeWebhookCommandHandler> logger)
    : IRequestHandler<ProcessStripeWebhookCommand, bool>
{
    public async Task<bool> Handle(ProcessStripeWebhookCommand request, CancellationToken ct)
    {
        // For the stub, we attempt to find the payment intent ID in the JSON
        // In a real implementation, we would use Stripe.net to parse the event.
        var intentId = ExtractIntentId(request.DataJson);
        if (string.IsNullOrEmpty(intentId))
        {
            logger.LogWarning("Webhook {EventId} (type {Type}) received but no PaymentIntent ID found in payload.", request.EventId, request.EventType);
            return true; 
        }

        var payment = await db.CompanyReviewPayments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == intentId, ct);

        if (payment == null)
        {
            // Might be a different type of payment (e.g., ConsultantBooking)
            // For PB-005 we focus on CompanyReviewPayment
            logger.LogInformation("Webhook {EventId} received for unknown or non-company payment {IntentId}", request.EventId, intentId);
            return true;
        }

        if (request.EventType == "payment_intent.succeeded" || request.EventType == "payment_intent.amount_capturable_updated")
        {
            if (payment.Status == PaymentStatus.Pending)
            {
                payment.Status = PaymentStatus.Held;
                logger.LogInformation("Payment {IntentId} status updated to Held via webhook", intentId);
            }
        }
        else if (request.EventType == "charge.refunded")
        {
            payment.Status = PaymentStatus.Refunded;
            logger.LogInformation("Payment {IntentId} status updated to Refunded via webhook", intentId);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    private string? ExtractIntentId(string json)
    {
        // Simple extraction for stub/demo purposes
        // "id": "pi_..."
        var match = System.Text.RegularExpressions.Regex.Match(json, "\"id\":\\s*\"(pi_[^\"]+)\"");
        return match.Success ? match.Groups[1].Value : null;
    }
}
