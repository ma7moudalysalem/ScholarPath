using FluentValidation.TestHelper;
using ScholarPath.Application.Applications.Commands.CreateApplication;
using Xunit;

namespace ScholarPath.UnitTests.Applications;

public class CreateApplicationValidatorTests
{
    private readonly CreateApplicationCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_ScholarshipId_Is_Empty()
    {
        var command = new CreateApplicationCommand(Guid.Empty, "Some notes");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ScholarshipId);
    }

    [Fact]
    public void Should_Have_Error_When_PersonalNotes_Exceed_Maximum_Length()
    {
        var longNotes = new string('a', 4001);
        var command = new CreateApplicationCommand(Guid.NewGuid(), longNotes);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PersonalNotes);
    }
}
