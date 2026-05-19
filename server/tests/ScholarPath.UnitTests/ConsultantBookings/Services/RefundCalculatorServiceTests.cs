using ScholarPath.Application.ConsultantBookings.Services;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Exceptions;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings.Services;

public sealed class RefundCalculatorServiceTests
{
    private static readonly Guid StudentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ConsultantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly RefundCalculatorService _sut = new();

    [Fact]
    public void Calculate_WhenStudentCancelsRequestedBooking_ReturnsFullRefund()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 25, 10, 0, 0, TimeSpan.Zero);

        var result = _sut.Calculate(
            bookingStatus: BookingStatus.Requested,
            cancelledByUserId: StudentId,
            studentId: StudentId,
            consultantId: ConsultantId,
            scheduledStartAt: nowUtc.AddDays(2),
            amountCents: 10_000,
            nowUtc: nowUtc);

        Assert.Equal(100, result.RefundPercentage);
        Assert.Equal(10_000, result.RefundAmountCents);
        Assert.Equal(CancellationReason.StudentCancelledBeforeAcceptance, result.CancellationReason);
    }

    [Fact]
    public void Calculate_WhenStudentCancelsConfirmedBookingMoreThan24HoursBefore_ReturnsFullRefund()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 25, 10, 0, 0, TimeSpan.Zero);

        var result = _sut.Calculate(
            bookingStatus: BookingStatus.Confirmed,
            cancelledByUserId: StudentId,
            studentId: StudentId,
            consultantId: ConsultantId,
            scheduledStartAt: nowUtc.AddHours(48),
            amountCents: 10_000,
            nowUtc: nowUtc);

        Assert.Equal(100, result.RefundPercentage);
        Assert.Equal(10_000, result.RefundAmountCents);
        Assert.Equal(CancellationReason.StudentCancelledMoreThan24HoursBefore, result.CancellationReason);
    }

    [Fact]
    public void Calculate_WhenStudentCancelsConfirmedBookingExactly24HoursBefore_ReturnsHalfRefund()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 25, 10, 0, 0, TimeSpan.Zero);

        var result = _sut.Calculate(
            bookingStatus: BookingStatus.Confirmed,
            cancelledByUserId: StudentId,
            studentId: StudentId,
            consultantId: ConsultantId,
            scheduledStartAt: nowUtc.AddHours(24),
            amountCents: 10_000,
            nowUtc: nowUtc);

        Assert.Equal(50, result.RefundPercentage);
        Assert.Equal(5_000, result.RefundAmountCents);
        Assert.Equal(CancellationReason.StudentCancelledLessThan24HoursBefore, result.CancellationReason);
    }

    [Fact]
    public void Calculate_WhenStudentCancelsConfirmedBookingLessThan24HoursBefore_ReturnsHalfRefund()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 25, 10, 0, 0, TimeSpan.Zero);

        var result = _sut.Calculate(
            bookingStatus: BookingStatus.Confirmed,
            cancelledByUserId: StudentId,
            studentId: StudentId,
            consultantId: ConsultantId,
            scheduledStartAt: nowUtc.AddHours(12),
            amountCents: 10_000,
            nowUtc: nowUtc);

        Assert.Equal(50, result.RefundPercentage);
        Assert.Equal(5_000, result.RefundAmountCents);
        Assert.Equal(CancellationReason.StudentCancelledLessThan24HoursBefore, result.CancellationReason);
    }

    [Fact]
    public void Calculate_WhenConsultantCancelsConfirmedBooking_ReturnsFullRefund()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 25, 10, 0, 0, TimeSpan.Zero);

        var result = _sut.Calculate(
            bookingStatus: BookingStatus.Confirmed,
            cancelledByUserId: ConsultantId,
            studentId: StudentId,
            consultantId: ConsultantId,
            scheduledStartAt: nowUtc.AddHours(12),
            amountCents: 10_000,
            nowUtc: nowUtc);

        Assert.Equal(100, result.RefundPercentage);
        Assert.Equal(10_000, result.RefundAmountCents);
        Assert.Equal(CancellationReason.ConsultantCancelledAfterAcceptance, result.CancellationReason);
    }

    [Fact]
    public void Calculate_WhenAmountHasOddCents_HalfRefundFloorsToWholeCent()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 25, 10, 0, 0, TimeSpan.Zero);

        // 9_999 / 2 = 4_999.5 -> integer division floors to 4_999.
        var result = _sut.Calculate(
            bookingStatus: BookingStatus.Confirmed,
            cancelledByUserId: StudentId,
            studentId: StudentId,
            consultantId: ConsultantId,
            scheduledStartAt: nowUtc.AddHours(12),
            amountCents: 9_999,
            nowUtc: nowUtc);

        Assert.Equal(50, result.RefundPercentage);
        Assert.Equal(4_999, result.RefundAmountCents);
        Assert.Equal(CancellationReason.StudentCancelledLessThan24HoursBefore, result.CancellationReason);
    }

    [Fact]
    public void Calculate_WhenAmountIsZero_ReturnsZeroRefundWithoutThrowing()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 25, 10, 0, 0, TimeSpan.Zero);

        var result = _sut.Calculate(
            bookingStatus: BookingStatus.Requested,
            cancelledByUserId: StudentId,
            studentId: StudentId,
            consultantId: ConsultantId,
            scheduledStartAt: nowUtc.AddDays(2),
            amountCents: 0,
            nowUtc: nowUtc);

        Assert.Equal(100, result.RefundPercentage);
        Assert.Equal(0, result.RefundAmountCents);
        Assert.Equal(CancellationReason.StudentCancelledBeforeAcceptance, result.CancellationReason);
    }

    [Fact]
    public void Calculate_WhenCancellingUserIsNotBookingParty_Throws()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 25, 10, 0, 0, TimeSpan.Zero);

        var ex = Assert.Throws<BookingDomainException>(() => _sut.Calculate(
            bookingStatus: BookingStatus.Confirmed,
            cancelledByUserId: OtherUserId,
            studentId: StudentId,
            consultantId: ConsultantId,
            scheduledStartAt: nowUtc.AddHours(12),
            amountCents: 10_000,
            nowUtc: nowUtc));

        Assert.Equal("Cancelling user is not related to this booking.", ex.Message);
    }

    [Fact]
    public void Calculate_WhenConsultantCancelsRequestedBooking_Throws()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 25, 10, 0, 0, TimeSpan.Zero);

        var ex = Assert.Throws<BookingDomainException>(() => _sut.Calculate(
            bookingStatus: BookingStatus.Requested,
            cancelledByUserId: ConsultantId,
            studentId: StudentId,
            consultantId: ConsultantId,
            scheduledStartAt: nowUtc.AddDays(2),
            amountCents: 10_000,
            nowUtc: nowUtc));

        Assert.Equal("Requested bookings can only be cancelled by the student. Consultants should reject them.", ex.Message);
    }

    [Fact]
    public void Calculate_WhenBookingStatusIsNotRequestedOrConfirmed_Throws()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 25, 10, 0, 0, TimeSpan.Zero);

        var ex = Assert.Throws<BookingDomainException>(() => _sut.Calculate(
            bookingStatus: BookingStatus.NoShowStudent,
            cancelledByUserId: StudentId,
            studentId: StudentId,
            consultantId: ConsultantId,
            scheduledStartAt: nowUtc.AddHours(-2),
            amountCents: 10_000,
            nowUtc: nowUtc));

        Assert.Equal("Only requested or confirmed bookings can be cancelled.", ex.Message);
    }

    [Fact]
    public void Calculate_WhenAmountIsNegative_Throws()
    {
        var nowUtc = new DateTimeOffset(2026, 4, 25, 10, 0, 0, TimeSpan.Zero);

        var ex = Assert.Throws<BookingDomainException>(() => _sut.Calculate(
            bookingStatus: BookingStatus.Confirmed,
            cancelledByUserId: StudentId,
            studentId: StudentId,
            consultantId: ConsultantId,
            scheduledStartAt: nowUtc.AddHours(12),
            amountCents: -100,
            nowUtc: nowUtc));

        Assert.Equal("Booking amount cannot be negative.", ex.Message);
    }
}
