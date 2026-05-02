using FluentValidation;

namespace ScholarPath.Application.ConsultantBookings.Commands.RejectBooking;

public sealed class RejectBookingCommandValidator : AbstractValidator<RejectBookingCommand>
{
    public RejectBookingCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty();
    }
}
