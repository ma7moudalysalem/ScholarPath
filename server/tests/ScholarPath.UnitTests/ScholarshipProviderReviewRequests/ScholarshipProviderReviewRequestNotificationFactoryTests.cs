using FluentAssertions;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Common;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using Xunit;

namespace ScholarPath.UnitTests.ScholarshipProviderReviewRequests;

/// <summary>
/// The notification factory turns a ScholarshipProviderReviewRequest + Payment pair into
/// the structured parameters every notification template renders. Money
/// fields render as "0.00 USD" by default — for free requests (no Payment
/// row, snapshot fee = 0) we surface "Free" instead so recipients don't read
/// a transaction that never happened.
/// </summary>
public class ScholarshipProviderReviewRequestNotificationFactoryTests
{
    private static ScholarshipProviderReviewRequest BaseRequest(decimal feeSnapshot, string currency = "USD") => new()
    {
        Id = Guid.NewGuid(),
        ScholarshipId = Guid.NewGuid(),
        StudentId = Guid.NewGuid(),
        ScholarshipProviderId = Guid.NewGuid(),
        Status = ScholarshipProviderReviewRequestStatus.Pending,
        ReviewFeeUsdSnapshot = feeSnapshot,
        Currency = currency,
        SubmittedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void Free_request_renders_Free_in_every_amount_field()
    {
        var request = BaseRequest(feeSnapshot: 0m);

        var p = ScholarshipProviderReviewRequestNotificationFactory.Build(
            request,
            payment: null,
            scholarshipTitleEn: "Test scholarship",
            scholarshipTitleAr: "منحة اختبارية",
            counterpartyName: "Acme");

        p.AmountText.Should().Be("Free");
        p.HeldAmountText.Should().Be("Free");
        p.CapturedAmountText.Should().Be("Free");
        p.RetainedAmountText.Should().Be("Free");
        p.PlatformCommissionText.Should().Be("Free");
        p.ScholarshipProviderShareText.Should().Be("Free");
        // Refund is null when nothing has been refunded — same as before.
        p.RefundAmountText.Should().BeNull();
    }

    [Fact]
    public void Paid_request_still_renders_formatted_amounts()
    {
        var request = BaseRequest(feeSnapshot: 150m);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.ScholarshipProviderReview,
            Status = PaymentStatus.Held,
            AmountCents = 15_000,
            Currency = "USD",
            ProfitShareAmountCents = 1_500,
            PayeeAmountCents = 13_500,
            RefundedAmountCents = 0,
            PayerUserId = request.StudentId,
            PayeeUserId = request.ScholarshipProviderId,
            StripePaymentIntentId = "pi_held",
        };

        var p = ScholarshipProviderReviewRequestNotificationFactory.Build(
            request, payment, "EN", "AR", "Counterparty");

        p.AmountText.Should().Be("150.00 USD");
        p.HeldAmountText.Should().Be("150.00 USD");
        p.PlatformCommissionText.Should().Be("15.00 USD");
        p.ScholarshipProviderShareText.Should().Be("135.00 USD");
        p.AmountText.Should().NotBe("Free");
    }
}
