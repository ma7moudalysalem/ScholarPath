using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ScholarPath.Application.Admin.Commands.ResolveNoShowReport;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.Services;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>
/// PB-006R (FR-CBR-25..32): the four admin-resolution branches of a no-show report —
/// validated student / validated consultant / falsely-reported consultant /
/// falsely-reported student — each apply the correct block, rating deduction, and refund.
/// </summary>
public sealed class ResolveNoShowReportTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IStripeService _stripe = Substitute.For<IStripeService>();
    private readonly ICurrentUserService _admin = Substitute.For<ICurrentUserService>();
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _consultantId = Guid.NewGuid();

    public ResolveNoShowReportTests()
    {
        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        _admin.IsInRole("Admin").Returns(true);
        _admin.UserId.Returns(Guid.NewGuid());
        _stripe.RefundPaymentAsync(Arg.Any<string>(), Arg.Any<long?>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult("re_test", "succeeded", 10000));
    }

    private ResolveNoShowReportCommandHandler Sut()
    {
        var ratingService = new ConsultantRatingService(
            _db, Substitute.For<INotificationDispatcher>(),
            NullLogger<ConsultantRatingService>.Instance);
        return new ResolveNoShowReportCommandHandler(
            _db, _admin, _stripe, ratingService, Substitute.For<INotificationDispatcher>(),
            Options.Create(new BookingOptions()), NullLogger<ResolveNoShowReportCommandHandler>.Instance);
    }

    private async Task<Guid> SeedAsync(NoShowAccusedRole accusedRole, bool withPayment = false)
    {
        _db.UserProfiles.Add(new UserProfile { UserId = _studentId });
        _db.UserProfiles.Add(new UserProfile { UserId = _consultantId });

        var booking = new ConsultantBooking
        {
            Id = Guid.NewGuid(),
            StudentId = _studentId,
            ConsultantId = _consultantId,
            Status = BookingStatus.NoShowReported,
            ScheduledStartAt = DateTimeOffset.UtcNow.AddHours(-2),
            ScheduledEndAt = DateTimeOffset.UtcNow.AddHours(-1),
            DurationMinutes = 45,
            PriceUsd = withPayment ? 100m : 0m,
            StripePaymentIntentId = withPayment ? "pi_test" : null,
        };
        if (withPayment)
        {
            booking.Payment = new Payment
            {
                Id = Guid.NewGuid(),
                Type = PaymentType.ConsultantBooking,
                Status = PaymentStatus.Captured,
                AmountCents = 10000,
                Currency = "USD",
                IdempotencyKey = "ik-" + Guid.NewGuid().ToString("N"),
                PayerUserId = _studentId,
                PayeeAmountCents = 9000,
                ProfitShareAmountCents = 1000,
            };
        }
        _db.Bookings.Add(booking);

        var (reporterId, accusedId) = accusedRole == NoShowAccusedRole.Consultant
            ? (_studentId, _consultantId)
            : (_consultantId, _studentId);

        var report = new NoShowReport
        {
            BookingId = booking.Id,
            ReporterUserId = reporterId,
            AccusedUserId = accusedId,
            AccusedRole = accusedRole,
            Status = NoShowReportStatus.PendingReview,
        };
        _db.NoShowReports.Add(report);
        await _db.SaveChangesAsync();
        return report.Id;
    }

    private async Task<UserProfile> ProfileAsync(Guid userId) =>
        await _db.UserProfiles.AsNoTracking().FirstAsync(p => p.UserId == userId);

    [Fact]
    public async Task Validated_student_no_show_blocks_the_student_7_days_and_forfeits_the_fee()
    {
        var reportId = await SeedAsync(NoShowAccusedRole.Student);

        await Sut().Handle(new ResolveNoShowReportCommand(reportId, IsValid: true, "confirmed"), default);

        var student = await ProfileAsync(_studentId);
        student.BookingAccessStatus.Should().Be(BookingAccessStatus.BookingBlocked);
        student.BookingBlockReason.Should().Be(BookingBlockReason.ValidatedNoShow);
        student.BookingBlockUntil.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(7), TimeSpan.FromMinutes(2));

        var booking = await _db.Bookings.AsNoTracking().FirstAsync();
        booking.Status.Should().Be(BookingStatus.NoShowStudent);
        (await _db.NoShowReports.AsNoTracking().FirstAsync()).Status
            .Should().Be(NoShowReportStatus.ValidatedNoShow);
        await _stripe.DidNotReceiveWithAnyArgs().RefundPaymentAsync(default!, default, default!, default!, default);
    }

    [Fact]
    public async Task Validated_consultant_no_show_deducts_40_percent_and_refunds_the_student()
    {
        var reportId = await SeedAsync(NoShowAccusedRole.Consultant, withPayment: true);

        await Sut().Handle(new ResolveNoShowReportCommand(reportId, IsValid: true, null), default);

        var consultant = await ProfileAsync(_consultantId);
        consultant.ConsultantRatingPenaltyFactor.Should().Be(0.60m); // −40%

        var booking = await _db.Bookings.AsNoTracking().FirstAsync();
        booking.Status.Should().Be(BookingStatus.NoShowConsultant);
        var payment = await _db.Payments.AsNoTracking().FirstAsync();
        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.PayeeAmountCents.Should().Be(0);
    }

    [Fact]
    public async Task False_report_by_student_blocks_the_reporting_student_14_days()
    {
        // Student accused the consultant; admin rejects it as false → reporter (student) blocked.
        var reportId = await SeedAsync(NoShowAccusedRole.Consultant);

        await Sut().Handle(new ResolveNoShowReportCommand(reportId, IsValid: false, "no evidence"), default);

        var student = await ProfileAsync(_studentId);
        student.BookingAccessStatus.Should().Be(BookingAccessStatus.BookingBlocked);
        student.BookingBlockReason.Should().Be(BookingBlockReason.FalseNoShowReport);
        student.BookingBlockUntil.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(14), TimeSpan.FromMinutes(2));

        (await _db.Bookings.AsNoTracking().FirstAsync()).Status.Should().Be(BookingStatus.Completed);
        (await _db.NoShowReports.AsNoTracking().FirstAsync()).Status
            .Should().Be(NoShowReportStatus.RejectedAsFalse);
    }

    [Fact]
    public async Task False_report_by_consultant_deducts_70_percent_from_the_consultant()
    {
        // Consultant accused the student; admin rejects it as false → reporter (consultant) penalised.
        var reportId = await SeedAsync(NoShowAccusedRole.Student);

        await Sut().Handle(new ResolveNoShowReportCommand(reportId, IsValid: false, null), default);

        var consultant = await ProfileAsync(_consultantId);
        consultant.ConsultantRatingPenaltyFactor.Should().Be(0.30m); // −70%
        (await _db.Bookings.AsNoTracking().FirstAsync()).Status.Should().Be(BookingStatus.Completed);
    }

    [Fact]
    public async Task Resolving_an_already_resolved_report_conflicts()
    {
        var reportId = await SeedAsync(NoShowAccusedRole.Student);
        await Sut().Handle(new ResolveNoShowReportCommand(reportId, IsValid: true, null), default);

        var act = () => Sut().Handle(new ResolveNoShowReportCommand(reportId, IsValid: true, null), default);

        await act.Should().ThrowAsync<ScholarPath.Application.Common.Exceptions.ConflictException>();
    }

    public void Dispose() => _db.Dispose();
}
