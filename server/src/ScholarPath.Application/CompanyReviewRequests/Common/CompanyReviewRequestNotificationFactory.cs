using System.Globalization;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.CompanyReviewRequests.Common;

/// <summary>
/// Builds <see cref="NotificationParams"/> for the CompanyReview request
/// lifecycle. Centralises the safe-data formatting (no PCI fields, pre-formatted
/// money/timestamp strings) so every handler queues notifications with the same
/// shape and both channels (in-app and email) render the same numbers.
/// </summary>
internal static class CompanyReviewRequestNotificationFactory
{
    /// <summary>
    /// Placeholder used in every amount field when a request is free (no
    /// Payment row, snapshot fee = 0). Picking a single neutral token keeps
    /// the in-app + e-mail templates locale-stable; the recipient's client
    /// can swap it for a translated label at render time if needed.
    /// </summary>
    public const string FreeAmountToken = "Free";

    public static NotificationParams Build(
        CompanyReviewRequest request,
        Payment? payment,
        string? scholarshipTitleEn,
        string? scholarshipTitleAr,
        string? counterpartyName)
    {
        var currency = payment?.Currency ?? request.Currency;
        var amountCents = payment?.AmountCents ?? FeeUsdToCents(request.ReviewFeeUsdSnapshot, currency);
        var refundedCents = payment?.RefundedAmountCents ?? 0;
        var retainedCents = Math.Max(0, amountCents - refundedCents);

        // Held = on-hold but not captured; Captured = money has actually moved.
        var heldCents = payment is { Status: Domain.Enums.PaymentStatus.Held } ? amountCents : 0;
        var capturedCents = payment is
        {
            Status: Domain.Enums.PaymentStatus.Captured
                or Domain.Enums.PaymentStatus.PartiallyRefunded
                or Domain.Enums.PaymentStatus.Refunded
        } ? amountCents : 0;

        // A request is "free" when it never had a Payment row attached — i.e.
        // the Apply Now flow short-circuited Stripe entirely. We surface that
        // explicitly in every money field so the recipient doesn't read
        // "0.00 USD" for a transaction that simply never happened.
        var isFree = payment is null && amountCents == 0;

        return new NotificationParams
        {
            ScholarshipNameEn = scholarshipTitleEn,
            ScholarshipNameAr = scholarshipTitleAr,
            CounterpartyName = counterpartyName,
            RequestStatusText = request.Status.ToString(),
            PaymentStatusText = payment?.Status.ToString(),
            PaymentReference = payment?.StripePaymentIntentId,
            TransactionAtText = (payment?.UpdatedAt
                ?? request.UpdatedAt
                ?? DateTimeOffset.UtcNow)
                .UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture),
            HeldAmountText = isFree ? FreeAmountToken : FormatCents(heldCents, currency),
            CapturedAmountText = isFree ? FreeAmountToken : FormatCents(capturedCents, currency),
            RefundAmountText = refundedCents > 0 ? FormatCents(refundedCents, currency) : null,
            RetainedAmountText = isFree ? FreeAmountToken : FormatCents(retainedCents, currency),
            PlatformCommissionText = payment is not null
                ? FormatCents(payment.ProfitShareAmountCents, currency)
                : (isFree ? FreeAmountToken : null),
            CompanyShareText = payment is not null
                ? FormatCents(payment.PayeeAmountCents, currency)
                : (isFree ? FreeAmountToken : null),
            AmountText = isFree ? FreeAmountToken : FormatCents(amountCents, currency),
        };
    }

    /// <summary>
    /// USD-decimal → cents with banker's rounding. Used only as a fallback when
    /// the Payment row hasn't been created yet (Draft) so the catalog still
    /// has a gross-fee figure to interpolate.
    /// </summary>
    private static long FeeUsdToCents(decimal usd, string currency) =>
        string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase)
            ? (long)Math.Round(usd * 100m, MidpointRounding.AwayFromZero)
            : (long)Math.Round(usd * 100m, MidpointRounding.AwayFromZero);

    private static string FormatCents(long cents, string currency)
    {
        var amount = cents / 100m;
        // CurrentCulture is invariant on the server; the recipient's app
        // re-formats client-side for their locale. Sending a stable string
        // here keeps the e-mail template free of locale surprises.
        return string.Format(
            CultureInfo.InvariantCulture, "{0:0.00} {1}", amount, currency);
    }
}
