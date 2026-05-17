using FluentValidation;

namespace ScholarPath.Application.ConsultantBookings.Commands.RescheduleBooking;

public sealed class RescheduleBookingCommandValidator : AbstractValidator<RescheduleBookingCommand>
{
    public RescheduleBookingCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty();

        RuleFor(x => x.ScheduledStartAt)
            .NotEmpty();

        RuleFor(x => x.ScheduledEndAt)
            .NotEmpty()
            .GreaterThan(x => x.ScheduledStartAt)
            .WithMessage("ScheduledEndAt must be later than ScheduledStartAt.");
    }
}
