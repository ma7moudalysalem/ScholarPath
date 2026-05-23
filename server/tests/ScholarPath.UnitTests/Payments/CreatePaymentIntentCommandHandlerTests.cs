using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Payments.Commands.CreatePaymentIntent;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Payments;

/// <summary>
/// PB-005 / PB-013: every payment intent must be created with manual capture
/// so the counterparty (consultant or company) actually has to accept before
/// the student is charged. PayeeUserId for a CompanyReview intent is resolved
/// server-side from the scholarship's owning company.
/// </summary>
public class CreatePaymentIntentCommandHandlerTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ICurrentUserService AuthenticatedUser(Guid id)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.UserId.Returns(id);
        return u;
    }

    [Fact]
    public async Task ConsultantBooking_intent_is_created_with_manual_capture()
    {
        using var db = CreateDb();
        var stripe = Substitute.For<IStripeService>();
        stripe.CreatePaymentIntentAsync(
                Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IDictionary<string, string>?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult("pi_b", "requires_payment_method", "cs_b", null));

        var handler = new CreatePaymentIntentCommandHandler(
            db, stripe, AuthenticatedUser(Guid.NewGuid()),
            NullLogger<CreatePaymentIntentCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreatePaymentIntentCommand(
                PaymentType.ConsultantBooking,
                AmountCents: 10_000,
                Currency: "USD",
                PayeeUserId: Guid.NewGuid(),
                RelatedBookingId: Guid.NewGuid(),
                RelatedApplicationId: null),
            default);

        result.PaymentId.Should().NotBe(Guid.Empty);
        await stripe.Received(1).CreatePaymentIntentAsync(
            10_000, "usd", "manual",
            Arg.Any<IDictionary<string, string>?>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompanyReview_intent_is_created_with_manual_capture_not_automatic()
    {
        using var db = CreateDb();
        var companyId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var scholarshipId = Guid.NewGuid();
        db.Scholarships.Add(new Scholarship
        {
            Id = scholarshipId,
            TitleEn = "S",
            TitleAr = "س",
            Slug = $"s-{Guid.NewGuid():N}",
            DescriptionEn = "d",
            DescriptionAr = "د",
            Deadline = DateTimeOffset.UtcNow.AddDays(30),
            OwnerCompanyId = companyId,
            ReviewFeeUsd = 50m,
        });
        db.Applications.Add(new ApplicationTracker
        {
            Id = appId,
            StudentId = Guid.NewGuid(),
            ScholarshipId = scholarshipId,
            Status = ApplicationStatus.Draft,
        });
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.CreatePaymentIntentAsync(
                Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IDictionary<string, string>?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult("pi_cr", "requires_payment_method", "cs_cr", null));

        var handler = new CreatePaymentIntentCommandHandler(
            db, stripe, AuthenticatedUser(Guid.NewGuid()),
            NullLogger<CreatePaymentIntentCommandHandler>.Instance);

        await handler.Handle(
            new CreatePaymentIntentCommand(
                PaymentType.CompanyReview,
                AmountCents: 5_000,
                Currency: "USD",
                PayeeUserId: null,
                RelatedBookingId: null,
                RelatedApplicationId: appId),
            default);

        await stripe.Received(1).CreatePaymentIntentAsync(
            5_000, "usd", "manual",
            Arg.Any<IDictionary<string, string>?>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());

        // PayeeUserId must be the scholarship's owning company, NOT the
        // (null) PayeeUserId supplied in the command.
        var stored = await db.Payments.SingleAsync();
        stored.PayeeUserId.Should().Be(companyId);
        stored.Type.Should().Be(PaymentType.CompanyReview);
        stored.RelatedApplicationId.Should().Be(appId);
        stored.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task CompanyReview_throws_when_scholarship_has_no_owning_company()
    {
        using var db = CreateDb();
        var appId = Guid.NewGuid();
        var scholarshipId = Guid.NewGuid();
        db.Scholarships.Add(new Scholarship
        {
            Id = scholarshipId,
            TitleEn = "S", TitleAr = "س",
            Slug = $"s-{Guid.NewGuid():N}",
            DescriptionEn = "d", DescriptionAr = "د",
            Deadline = DateTimeOffset.UtcNow.AddDays(30),
            OwnerCompanyId = null, // admin-created, no company
        });
        db.Applications.Add(new ApplicationTracker
        {
            Id = appId,
            StudentId = Guid.NewGuid(),
            ScholarshipId = scholarshipId,
            Status = ApplicationStatus.Draft,
        });
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        var handler = new CreatePaymentIntentCommandHandler(
            db, stripe, AuthenticatedUser(Guid.NewGuid()),
            NullLogger<CreatePaymentIntentCommandHandler>.Instance);

        var act = () => handler.Handle(
            new CreatePaymentIntentCommand(
                PaymentType.CompanyReview,
                AmountCents: 1_000,
                Currency: "USD",
                PayeeUserId: null,
                RelatedBookingId: null,
                RelatedApplicationId: appId),
            default);

        await act.Should().ThrowAsync<ConflictException>();
        await stripe.DidNotReceiveWithAnyArgs().CreatePaymentIntentAsync(
            default, default!, default!, default, default!, default);
    }

    [Fact]
    public void Validator_requires_RelatedApplicationId_for_CompanyReview()
    {
        var validator = new CreatePaymentIntentCommandValidator();

        var result = validator.Validate(new CreatePaymentIntentCommand(
            PaymentType.CompanyReview, 100, "USD", null,
            RelatedBookingId: null,
            RelatedApplicationId: null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreatePaymentIntentCommand.RelatedApplicationId));
    }

    [Fact]
    public void Validator_requires_RelatedBookingId_for_ConsultantBooking()
    {
        var validator = new CreatePaymentIntentCommandValidator();

        var result = validator.Validate(new CreatePaymentIntentCommand(
            PaymentType.ConsultantBooking, 100, "USD", null,
            RelatedBookingId: null,
            RelatedApplicationId: null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreatePaymentIntentCommand.RelatedBookingId));
    }
}
