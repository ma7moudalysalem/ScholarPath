using ScholarPath.Application.ConsultantBookings.Commands.RescheduleBooking;
using Xunit;
using FluentAssertions;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>SRS FR-229 — reschedule-booking command validation.</summary>
public class RescheduleBookingValidatorTests
{
    private readonly RescheduleBookingCommandValidator _v = new();

    [Fact]
    public void Valid_request_passes()
    {
        var start = DateTimeOffset.UtcNow.AddDays(3);
        var r = _v.Validate(new RescheduleBookingCommand(
            Guid.NewGuid(), null, start, start.AddMinutes(60)));
        r.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_booking_id_fails()
    {
        var start = DateTimeOffset.UtcNow.AddDays(3);
        var r = _v.Validate(new RescheduleBookingCommand(
            Guid.Empty, null, start, start.AddMinutes(60)));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void End_before_start_fails()
    {
        var start = DateTimeOffset.UtcNow.AddDays(3);
        var r = _v.Validate(new RescheduleBookingCommand(
            Guid.NewGuid(), null, start, start.AddMinutes(-60)));
        r.IsValid.Should().BeFalse();
    }
}
