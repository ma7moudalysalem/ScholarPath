using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.Commands.AcceptBooking;
using ScholarPath.Application.ConsultantBookings.Commands.CancelBooking;
using ScholarPath.Application.ConsultantBookings.Commands.MarkNoShow;
using ScholarPath.Application.ConsultantBookings.Commands.RejectBooking;
using ScholarPath.Application.ConsultantBookings.Services;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Application.Common;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Jobs;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>
/// Asserts that the booking lifecycle handlers keep the internal Payment row in
/// sync with the Stripe transaction (PB-006 gap report, Problems 3-10).
/// </summary>
public sealed class BookingPaymentSyncTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IStripeService _stripe = Substitute.For<IStripeService>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _consultantId = Guid.NewGuid();

    public BookingPaymentSyncTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
    }

    private async Task<ConsultantBooking> SeedAsync(
        BookingStatus bookingStatus,
        PaymentStatus paymentStatus,
        DateTimeOffset start,
        DateTimeOffset? end = null,
        DateTimeOffset? requestedAt = null)
    {
        var bookingId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var payment = new Payment
        {
            Id = paymentId,
            Type = PaymentType.ConsultantBooking,
            Status = paymentStatus,
            AmountCents = 10_000,
            Currency = "USD",
            ProfitShareAmountCents = 0,
            PayeeAmountCents = 10_000,
            PayerUserId = _studentId,
            PayeeUserId = _consultantId,
            StripePaymentIntentId = "pi_test",
            IdempotencyKey = $"key:{bookingId:N}",
            RelatedBookingId = bookingId,
            HeldAt = DateTimeOffset.UtcNow.AddHours(-1),
        };

        var booking = new ConsultantBooking
        {
            Id = bookingId,
            StudentId = _studentId,
            ConsultantId = _consultantId,
            ScheduledStartAt = start,
            ScheduledEndAt = end ?? start.AddMinutes(60),
            DurationMinutes = 60,
            PriceUsd = 100m,
            Status = bookingStatus,
            RequestedAt = requestedAt ?? DateTimeOffset.UtcNow.AddHours(-1),
            StripePaymentIntentId = "pi_test",
            PaymentId = paymentId,
        };

        _db.Payments.Add(payment);
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        return booking;
    }

    private Task<Payment> PaymentForAsync(Guid bookingId) =>
        _db.Payments.AsNoTracking().FirstAsync(p => p.RelatedBookingId == bookingId);

    [Fact]
    public async Task Accept_marks_the_payment_Captured_and_locks_in_the_split()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.IsInRole("Consultant").Returns(true);
        _currentUser.UserId.Returns(_consultantId);
        var booking = await SeedAsync(
            BookingStatus.Requested, PaymentStatus.Held, DateTimeOffset.UtcNow.AddDays(2));
        _stripe.CapturePaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<long?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult("pi_test", "succeeded", null, "ch_test"));

        var handler = new AcceptBookingCommandHandler(_db, _currentUser, _stripe, new StubMeetingService(), _publisher);
        await handler.Handle(new AcceptBookingCommand(booking.Id, "https://meet.example/x"), default);

        var payment = await PaymentForAsync(booking.Id);
        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.CapturedAt.Should().NotBeNull();
        payment.StripeChargeId.Should().Be("ch_test");
        // Default ConsultantBooking profit-share is 10% of the 10_000 gross.
        payment.ProfitShareAmountCents.Should().Be(1_000);
        payment.PayeeAmountCents.Should().Be(9_000);
    }

    [Fact]
    public async Task Accept_captures_a_payment_still_Pending_when_the_Held_webhook_lagged()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.IsInRole("Consultant").Returns(true);
        _currentUser.UserId.Returns(_consultantId);
        // P2: payment starts Pending; the Stripe capture proves authorisation
        // even if the amount_capturable_updated webhook has not landed yet.
        var booking = await SeedAsync(
            BookingStatus.Requested, PaymentStatus.Pending, DateTimeOffset.UtcNow.AddDays(2));
        _stripe.CapturePaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<long?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult("pi_test", "succeeded", null, "ch_test"));

        var handler = new AcceptBookingCommandHandler(_db, _currentUser, _stripe, new StubMeetingService(), _publisher);
        await handler.Handle(new AcceptBookingCommand(booking.Id, "https://meet.example/x"), default);

        var payment = await PaymentForAsync(booking.Id);
        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.CapturedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Reject_marks_the_payment_Cancelled()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.IsInRole("Consultant").Returns(true);
        _currentUser.UserId.Returns(_consultantId);
        var booking = await SeedAsync(
            BookingStatus.Requested, PaymentStatus.Held, DateTimeOffset.UtcNow.AddDays(2));
        _stripe.CancelPaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult("pi_test", "canceled", null, null));

        var handler = new RejectBookingCommandHandler(_db, _currentUser, _stripe);
        await handler.Handle(new RejectBookingCommand(booking.Id), default);

        var payment = await PaymentForAsync(booking.Id);
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.FailureReason.Should().Be("rejected_by_consultant");
    }

    [Fact]
    public async Task Cancel_requested_booking_marks_the_payment_Cancelled()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_studentId);
        var booking = await SeedAsync(
            BookingStatus.Requested, PaymentStatus.Held, DateTimeOffset.UtcNow.AddDays(2));
        _stripe.CancelPaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult("pi_test", "canceled", null, null));

        var handler = new CancelBookingCommandHandler(
            _db, _currentUser, _stripe, new RefundCalculatorService(), _publisher);
        await handler.Handle(new CancelBookingCommand(booking.Id), default);

        var payment = await PaymentForAsync(booking.Id);
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.FailureReason.Should().Be("student_cancelled_before_acceptance");
    }

    [Fact]
    public async Task Cancel_confirmed_booking_marks_the_payment_Refunded()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_consultantId); // consultant cancels -> 100% refund
        var booking = await SeedAsync(
            BookingStatus.Confirmed, PaymentStatus.Captured, DateTimeOffset.UtcNow.AddDays(2));
        _stripe.RefundPaymentAsync(
                Arg.Any<string>(), Arg.Any<long?>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult("re_test", "succeeded", 10_000));

        var handler = new CancelBookingCommandHandler(
            _db, _currentUser, _stripe, new RefundCalculatorService(), _publisher);
        await handler.Handle(new CancelBookingCommand(booking.Id), default);

        var payment = await PaymentForAsync(booking.Id);
        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAmountCents.Should().Be(10_000);
        payment.RefundedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkNoShow_consultant_marks_the_payment_Refunded()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_studentId); // student reports the consultant did not show
        var start = DateTimeOffset.UtcNow.AddHours(-2);
        var booking = await SeedAsync(
            BookingStatus.Confirmed, PaymentStatus.Captured, start, start.AddHours(1));
        _stripe.RefundPaymentAsync(
                Arg.Any<string>(), Arg.Any<long?>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult("re_test", "succeeded", 10_000));

        var handler = new MarkNoShowCommandHandler(_db, _currentUser, _stripe);
        await handler.Handle(new MarkNoShowCommand(booking.Id), default);

        var payment = await PaymentForAsync(booking.Id);
        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAmountCents.Should().Be(10_000);
    }

    [Fact]
    public async Task MarkNoShow_student_leaves_the_payment_Captured()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_consultantId); // consultant reports the student did not show
        var start = DateTimeOffset.UtcNow.AddHours(-2);
        var booking = await SeedAsync(
            BookingStatus.Confirmed, PaymentStatus.Captured, start, start.AddHours(1));

        var handler = new MarkNoShowCommandHandler(_db, _currentUser, _stripe);
        await handler.Handle(new MarkNoShowCommand(booking.Id), default);

        var payment = await PaymentForAsync(booking.Id);
        payment.Status.Should().Be(PaymentStatus.Captured);
        await _stripe.DidNotReceive().RefundPaymentAsync(
            Arg.Any<string>(), Arg.Any<long?>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SessionExpiryJob_marks_an_expired_booking_payment_Cancelled()
    {
        var booking = await SeedAsync(
            BookingStatus.Requested, PaymentStatus.Held, DateTimeOffset.UtcNow.AddDays(2),
            requestedAt: DateTimeOffset.UtcNow.AddHours(-25));
        _stripe.CancelPaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult("pi_test", "canceled", null, null));

        var job = new SessionExpiryJob(
            _db, _stripe, Options.Create(new BookingOptions()),
            Substitute.For<ILogger<SessionExpiryJob>>());
        await job.RunAsync(default);

        var payment = await PaymentForAsync(booking.Id);
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.FailureReason.Should().Be("booking_request_expired");
    }

    public void Dispose() => _db.Dispose();
}
