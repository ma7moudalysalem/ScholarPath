using FluentValidation;

namespace ScholarPath.Application.ConsultantBookings.Commands.SubmitConsultantRating;

public sealed class SubmitConsultantRatingCommandValidator : AbstractValidator<SubmitConsultantRatingCommand>
{
    public SubmitConsultantRatingCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty();

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5);

        RuleFor(x => x.Comment)
            .MaximumLength(1000);
    }
}
