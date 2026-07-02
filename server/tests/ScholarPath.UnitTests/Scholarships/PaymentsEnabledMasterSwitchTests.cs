using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Start;
using ScholarPath.Application.Scholarships.Commands.ConfigureReviewFee;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.UnitTests.ScholarshipProviderReviewRequests;
using Xunit;

namespace ScholarPath.UnitTests.Scholarships;

/// <summary>
/// The master <c>payments.enabled</c> setting (PB-005R/PB-006R) puts the
/// platform in free mode. When off:
///   - Companies / consultants cannot set a positive fee (any value is
///     silently clamped to 0).
///   - Apply Now / Booking flows always take the free path regardless of any
///     fee that's already stored — Stripe is never called.
///
/// The two allow-free toggles are moot when the master switch is off; the
/// reader returns the master flag first and short-circuits.
/// </summary>
public class PaymentsEnabledMasterSwitchTests
{
    private static void SeedSetting(ApplicationDbContext db, string key, bool value)
    {
        db.PlatformSettings.Add(new PlatformSetting
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = value ? "true" : "false",
            ValueType = PlatformSettingType.Boolean,
            Category = "Payments",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task ConfigureReviewFee_forces_zero_when_master_payments_switch_is_off()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, _, company) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db, reviewFeeUsd: 50m);
        SeedSetting(db, PlatformSettingsKeys.PaymentsEnabled, value: false);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(company.Id);

        var sut = new ConfigureReviewFeeCommandHandler(
            db, currentUser, NullLogger<ConfigureReviewFeeCommandHandler>.Instance);

        // ScholarshipProvider sends a non-zero fee, but the master switch forces 0 silently.
        await sut.Handle(new ConfigureReviewFeeCommand(scholarship.Id, 120m), default);

        db.Scholarships.Single(s => s.Id == scholarship.Id)
            .ReviewFeeUsd.Should().Be(0m);
    }

    [Fact]
    public async Task ApplyNow_takes_free_path_when_master_switch_off_even_if_stored_fee_is_positive()
    {
        // Existing data: scholarship has fee=200 from before payments were turned off.
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, student, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db, reviewFeeUsd: 200m);
        SeedSetting(db, PlatformSettingsKeys.PaymentsEnabled, value: false);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(student.Id);
        var stripe = Substitute.For<IStripeService>();

        var sut = new StartScholarshipProviderReviewRequestCommandHandler(
            db, stripe, currentUser,
            NullLogger<StartScholarshipProviderReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(
            new StartScholarshipProviderReviewRequestCommand(scholarship.Id), default);

        result.IsFree.Should().BeTrue();
        result.PaymentId.Should().BeNull();
        result.ClientSecret.Should().BeNull();
        result.AmountCents.Should().Be(0);

        // Critically: Stripe must NOT be called — the master switch is the
        // platform-wide kill switch for any Stripe interaction.
        await stripe.DidNotReceiveWithAnyArgs().CreatePaymentIntentAsync(
            default, default!, default!, default!, default!, default);

        var entity = db.ScholarshipProviderReviewRequests.Single();
        entity.PaymentId.Should().BeNull();
        entity.ReviewFeeUsdSnapshot.Should().Be(0m);
        db.Payments.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyNow_remains_paid_when_master_switch_is_on()
    {
        // Sanity check: with master switch ON and a positive fee, the paid
        // flow runs as usual — the master switch only changes behaviour when off.
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, student, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db, reviewFeeUsd: 75m);
        SeedSetting(db, PlatformSettingsKeys.PaymentsEnabled, value: true);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(student.Id);
        var stripe = Substitute.For<IStripeService>();
        stripe.CreatePaymentIntentAsync(
                Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IDictionary<string, string>>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult(
                "pi_test", "requires_payment_method", "cs_test", null));

        var sut = new StartScholarshipProviderReviewRequestCommandHandler(
            db, stripe, currentUser,
            NullLogger<StartScholarshipProviderReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(
            new StartScholarshipProviderReviewRequestCommand(scholarship.Id), default);

        result.IsFree.Should().BeFalse();
        result.AmountCents.Should().Be(7_500);
        await stripe.Received(1).CreatePaymentIntentAsync(
            7_500, Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyNow_works_when_payments_off_and_stored_fee_is_null()
    {
        // Legacy listing: the ScholarshipProvider never configured a Review Service Fee
        // (ReviewFeeUsd is NULL on the row). When the master switch is OFF,
        // the platform treats every request as free, so this should NOT be
        // blocked by the "fee is not configured" guard.
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, student, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db, reviewFeeUsd: null);
        SeedSetting(db, PlatformSettingsKeys.PaymentsEnabled, value: false);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(student.Id);
        var stripe = Substitute.For<IStripeService>();

        var sut = new StartScholarshipProviderReviewRequestCommandHandler(
            db, stripe, currentUser,
            NullLogger<StartScholarshipProviderReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(
            new StartScholarshipProviderReviewRequestCommand(scholarship.Id), default);

        result.IsFree.Should().BeTrue();
        result.PaymentId.Should().BeNull();
        result.AmountCents.Should().Be(0);

        var entity = db.ScholarshipProviderReviewRequests.Single();
        entity.Status.Should().Be(ScholarshipProviderReviewRequestStatus.Pending);
        entity.PaymentId.Should().BeNull();
    }

    [Fact]
    public async Task ApplyNow_still_throws_when_payments_on_and_stored_fee_is_null()
    {
        // Inverse case: with payments ON, a missing fee remains a hard error —
        // the previous behaviour must not regress for paid-mode platforms.
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, student, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db, reviewFeeUsd: null);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(student.Id);

        var sut = new StartScholarshipProviderReviewRequestCommandHandler(
            db, Substitute.For<IStripeService>(), currentUser,
            NullLogger<StartScholarshipProviderReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(
            new StartScholarshipProviderReviewRequestCommand(scholarship.Id), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Review Service Fee*");
    }
}
