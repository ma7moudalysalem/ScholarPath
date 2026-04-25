using FluentValidation;

namespace ScholarPath.Application.ConsultantBookings.Commands.MarkNoShow;

public sealed class MarkNoShowCommandValidator : AbstractValidator<MarkNoShowCommand>
{
    public MarkNoShowCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty();
    }
}
