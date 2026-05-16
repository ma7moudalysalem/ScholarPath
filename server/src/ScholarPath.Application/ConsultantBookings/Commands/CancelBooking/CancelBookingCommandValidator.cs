using FluentValidation;

namespace ScholarPath.Application.ConsultantBookings.Commands.CancelBooking;

public sealed class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty();
    }
}
