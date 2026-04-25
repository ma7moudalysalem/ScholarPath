using FluentValidation;

namespace ScholarPath.Application.ConsultantBookings.Commands.RequestBooking;

public sealed class RequestBookingCommandValidator : AbstractValidator<RequestBookingCommand>
{
    public RequestBookingCommandValidator()
    {
        RuleFor(x => x.ConsultantId)
            .NotEmpty();

        RuleFor(x => x.ScheduledStartAt)
            .NotEmpty();

        RuleFor(x => x.ScheduledEndAt)
            .NotEmpty();

        RuleFor(x => x)
            .Must(x => x.ScheduledStartAt < x.ScheduledEndAt)
            .WithMessage("ScheduledStartAt must be earlier than ScheduledEndAt.");

        RuleFor(x => x.Timezone)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}
