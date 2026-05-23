using Microsoft.EntityFrameworkCore;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.UnitTests.CompanyReviewRequests;

/// <summary>
/// Shared fixture builders for the CompanyReviewRequest handler tests so each
/// test only declares what it actually cares about (status, payment state,
/// amount). Keeps the per-test seeding noise down.
/// </summary>
internal static class CompanyReviewRequestTestFixtures
{
    public static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    public static (Scholarship scholarship, ApplicationUser student, ApplicationUser company)
        SeedParticipants(ApplicationDbContext db, decimal? reviewFeeUsd = 100m)
    {
        var company = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Acme",
            LastName = "Foundation",
            UserName = $"acme-{Guid.NewGuid():N}@example.com",
            Email = $"acme-{Guid.NewGuid():N}@example.com",
        };
        var student = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Doe",
            UserName = $"jane-{Guid.NewGuid():N}@example.com",
            Email = $"jane-{Guid.NewGuid():N}@example.com",
        };
        var scholarship = new Scholarship
        {
            Id = Guid.NewGuid(),
            TitleEn = "Test scholarship",
            TitleAr = "منحة اختبارية",
            DescriptionEn = "x",
            DescriptionAr = "x",
            Slug = $"test-{Guid.NewGuid():N}",
            Mode = ListingMode.InApp,
            Status = ScholarshipStatus.Open,
            Deadline = DateTimeOffset.UtcNow.AddDays(30),
            OwnerCompanyId = company.Id,
            FundingType = FundingType.FullyFunded,
            TargetLevel = AcademicLevel.Undergrad,
            Currency = "USD",
            ReviewFeeUsd = reviewFeeUsd,
        };

        db.Users.AddRange(student, company);
        db.Scholarships.Add(scholarship);
        db.SaveChanges();
        return (scholarship, student, company);
    }

    /// <summary>
    /// Seeds a CompanyReviewRequest already in the given status with a paired
    /// Payment row in the matching state. Each status maps to the Payment
    /// state the production state machine would have placed it in.
    /// </summary>
    public static (CompanyReviewRequest request, Payment payment)
        SeedRequestWithPayment(
            ApplicationDbContext db,
            CompanyReviewRequestStatus status,
            long amountCents = 10_000,
            long refundedCents = 0,
            Guid? studentIdOverride = null,
            Guid? companyIdOverride = null,
            Guid? scholarshipIdOverride = null)
    {
        var (scholarship, student, company) = (
            scholarshipIdOverride, studentIdOverride, companyIdOverride) switch
        {
            (not null, not null, not null) => (
                db.Scholarships.First(s => s.Id == scholarshipIdOverride),
                db.Users.First(u => u.Id == studentIdOverride),
                db.Users.First(u => u.Id == companyIdOverride)),
            _ => SeedParticipants(db),
        };

        var paymentStatus = status switch
        {
            CompanyReviewRequestStatus.Draft => PaymentStatus.Pending,
            CompanyReviewRequestStatus.Submitted => PaymentStatus.Pending,
            CompanyReviewRequestStatus.Pending => PaymentStatus.Held,
            CompanyReviewRequestStatus.UnderReview => PaymentStatus.Captured,
            CompanyReviewRequestStatus.Completed => PaymentStatus.Captured,
            CompanyReviewRequestStatus.Closed => PaymentStatus.Captured,
            CompanyReviewRequestStatus.Cancelled => PaymentStatus.Cancelled,
            CompanyReviewRequestStatus.CancelledByStudent => refundedCents > 0
                ? PaymentStatus.PartiallyRefunded
                : PaymentStatus.Cancelled,
            CompanyReviewRequestStatus.RejectedByCompany => PaymentStatus.Cancelled,
            CompanyReviewRequestStatus.Expired => PaymentStatus.Cancelled,
            CompanyReviewRequestStatus.Failed => PaymentStatus.Failed,
            _ => PaymentStatus.Pending,
        };

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.CompanyReview,
            Status = paymentStatus,
            AmountCents = amountCents,
            Currency = "USD",
            ProfitShareAmountCents = amountCents / 10,
            PayeeAmountCents = amountCents - amountCents / 10,
            RefundedAmountCents = refundedCents,
            PayerUserId = student.Id,
            PayeeUserId = company.Id,
            StripePaymentIntentId = $"pi_test_{Guid.NewGuid():N}",
            IdempotencyKey = $"key_{Guid.NewGuid():N}",
            CapturedAt = paymentStatus is PaymentStatus.Captured
                or PaymentStatus.PartiallyRefunded
                or PaymentStatus.Refunded ? DateTimeOffset.UtcNow : null,
            HeldAt = paymentStatus == PaymentStatus.Held ? DateTimeOffset.UtcNow : null,
        };

        var request = new CompanyReviewRequest
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            CompanyId = company.Id,
            ScholarshipId = scholarship.Id,
            PaymentId = payment.Id,
            Status = status,
            ReviewFeeUsdSnapshot = amountCents / 100m,
            Currency = "USD",
            SubmittedAt = status >= CompanyReviewRequestStatus.Submitted
                ? DateTimeOffset.UtcNow : null,
            AcceptedAt = status >= CompanyReviewRequestStatus.UnderReview
                ? DateTimeOffset.UtcNow : null,
        };

        db.Payments.Add(payment);
        db.CompanyReviewRequests.Add(request);
        db.SaveChanges();
        return (request, payment);
    }
}
