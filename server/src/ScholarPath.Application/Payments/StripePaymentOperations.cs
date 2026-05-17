using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Payments;

/// <summary>
/// Shared Stripe operations used by both payment subsystems — the cents-based
/// <see cref="Domain.Entities.Payment"/> flow and the USD-based
/// <see cref="Domain.Entities.CompanyReviewPayment"/> flow.
/// </summary>
public static class StripePaymentOperations
{
    /// <summary>
    /// Cancels a held (uncaptured) PaymentIntent — releasing the card hold
    /// without ever charging it — and verifies Stripe confirmed the cancellation.
    /// Throws <see cref="ConflictException"/> when Stripe does not report
    /// <c>canceled</c>, so a caller never records a refund that never happened.
    /// </summary>
    public static async Task CancelHeldPaymentAsync(
        this IStripeService stripeService,
        string paymentIntentId,
        string idempotencyKey,
        CancellationToken ct)
    {
        var result = await stripeService.CancelPaymentIntentAsync(
            paymentIntentId: paymentIntentId,
            cancellationReason: "requested_by_customer",
            idempotencyKey: idempotencyKey,
            ct: ct).ConfigureAwait(false);

        if (result.Status != "canceled")
        {
            throw new ConflictException(
                $"Stripe cancellation did not succeed. Status: {result.Status}");
        }
    }
}
