using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;
using FluentAssertions;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.Commands.RequestBooking;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>
/// The consultant-booking side of the master <c>payments.enabled</c> switch
/// (the scholarship / Apply-Now side is covered by
/// <c>PaymentsEnabledMasterSwitchTests</c>). Proves both modes activate:
///   • switch OFF  → the booking is free, no Payment row, Stripe is never called.
///   • switch ON   → the booking is paid, a Stripe intent + Payment row are created.
/// </summary>
public sealed class RequestBookingMasterSwitchTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IStripeService _stripe = Substitute.For<IStripeService>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _consultantId = Guid.NewGuid();

    public RequestBookingMasterSwitchTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.IsInRole("Student").Returns(true);
        _currentUser.UserId.Returns(_studentId);
    }

    public void Dispose() => _db.Dispose();

    private void SeedConsultant(decimal fee)
    {
        _db.Users.Add(new ApplicationUser
        {
            Id = _consultantId,
            FirstName = "Sarah",
            LastName = "Adel",
            Email = "consultant@test.local",
            UserName = "consultant@test.local",
            AccountStatus = AccountStatus.Active,
            Profile = new UserProfile
            {
                UserId = _consultantId,
                SessionFeeUsd = fee,
                // Left null so the requested duration isn't rejected by the
                // session-duration-match guard — irrelevant to the fee path.
                SessionDurationMinutes = null,
            },
        });
        _db.SaveChanges();
    }

    private void SeedPaymentsEnabled(bool value)
    {
        _db.PlatformSettings.Add(new PlatformSetting
        {
            Id = Guid.NewGuid(),
            Key = PlatformSettingsKeys.PaymentsEnabled,
            Value = value ? "true" : "false",
            ValueType = PlatformSettingType.Boolean,
            Category = "Payments",
        });
        _db.SaveChanges();
    }

    private RequestBookingCommand FutureBooking()
    {
        var start = DateTimeOffset.UtcNow.AddDays(2);
        return new RequestBookingCommand(
            _consultantId, null, start, start.AddMinutes(45), "UTC", null);
    }

    private RequestBookingCommandHandler Sut() =>
        new(_db, _currentUser, _stripe, _publisher);

    [Fact]
    public async Task Master_switch_off_makes_the_booking_free_and_never_calls_Stripe()
    {
        SeedConsultant(fee: 50m);          // consultant DID set a positive fee…
        SeedPaymentsEnabled(false);        // …but the master switch is off.

        var result = await Sut().Handle(FutureBooking(), CancellationToken.None);

        result.IsFree.Should().BeTrue();
        result.ClientSecret.Should().BeNull();
        result.PaymentIntentId.Should().BeNull();

        var booking = _db.Bookings.Single();
        booking.PriceUsd.Should().Be(0m);
        booking.StripePaymentIntentId.Should().BeNull();
        booking.Status.Should().Be(BookingStatus.Requested);
        _db.Payments.Should().BeEmpty();

        // The platform-wide kill switch must bypass Stripe entirely.
        await _stripe.DidNotReceiveWithAnyArgs().CreatePaymentIntentAsync(
            default, default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task Master_switch_on_keeps_the_booking_paid_and_creates_a_Stripe_intent()
    {
        SeedConsultant(fee: 50m);
        SeedPaymentsEnabled(true);
        _stripe.CreatePaymentIntentAsync(
                Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IDictionary<string, string>>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult("pi_test", "requires_capture", "cs_test", null));

        var result = await Sut().Handle(FutureBooking(), CancellationToken.None);

        result.IsFree.Should().BeFalse();
        result.ClientSecret.Should().Be("cs_test");
        result.PaymentIntentId.Should().Be("pi_test");

        _db.Bookings.Single().PriceUsd.Should().Be(50m);

        var payment = _db.Payments.Single();
        payment.AmountCents.Should().Be(5_000);
        payment.Type.Should().Be(PaymentType.ConsultantBooking);

        await _stripe.Received(1).CreatePaymentIntentAsync(
            5_000, Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
