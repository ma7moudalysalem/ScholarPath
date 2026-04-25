using FluentValidation;

namespace ScholarPath.Application.ConsultantBookings.Commands.AcceptBooking;

public sealed class AcceptBookingCommandValidator : AbstractValidator<AcceptBookingCommand>
{
    public AcceptBookingCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty();

        RuleFor(x => x.MeetingUrl)
            .NotEmpty()
            .MaximumLength(2048);
    }
}
