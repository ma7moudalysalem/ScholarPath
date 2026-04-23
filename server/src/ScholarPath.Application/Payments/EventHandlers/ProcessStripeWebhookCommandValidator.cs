using MediatR;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Payments.Commands.ProcessStripeWebhook;

namespace ScholarPath.Application.Payments.EventHandlers;

public sealed class ProcessStripeWebhookCommandValidator
{
    // Idempotency verification happens via an IdempotencyKey check 
    // against the DB in a real application. T-006 mentions Idempotency.
}
