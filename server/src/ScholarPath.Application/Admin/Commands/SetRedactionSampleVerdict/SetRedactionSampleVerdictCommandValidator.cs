using FluentValidation;

namespace ScholarPath.Application.Admin.Commands.SetRedactionSampleVerdict;

public sealed class SetRedactionSampleVerdictCommandValidator : AbstractValidator<SetRedactionSampleVerdictCommand>
{
    public SetRedactionSampleVerdictCommandValidator()
    {
        RuleFor(x => x.SampleId).NotEmpty();
        RuleFor(x => x.Verdict).IsInEnum();
    }
}
