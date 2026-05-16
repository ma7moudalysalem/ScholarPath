using FluentAssertions;
using FluentValidation.TestHelper;
using ScholarPath.Application.Applications.Commands.StartApplication;
using ScholarPath.Application.Applications.Common;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using Xunit;

namespace ScholarPath.UnitTests.Applications;

public class CreateApplicationValidatorTests
{
    private readonly StartApplicationCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_ScholarshipId_Is_Empty()
    {
        var command = new StartApplicationCommand(Guid.Empty, "Some notes");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ScholarshipId);
    }

    [Fact]
    public void Should_Have_Error_When_PersonalNotes_Exceed_Maximum_Length()
    {
        var command = new StartApplicationCommand(Guid.NewGuid(), new string('a', 4001));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PersonalNotes);
    }

    [Fact]
    public void Should_Not_Have_Error_When_PersonalNotes_Is_Null()
    {
        var command = new StartApplicationCommand(Guid.NewGuid(), null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.PersonalNotes);
    }

    [Fact]
    public void Should_Not_Have_Error_When_PersonalNotes_Is_Exactly_4000_Chars()
    {
        var command = new StartApplicationCommand(Guid.NewGuid(), new string('a', 4000));
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.PersonalNotes);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

public class ApplicationStateMachineTests
{
    // ── Allowed transitions ───────────────────────────────────────────────────

    [Theory]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Pending)]
    [InlineData(ApplicationStatus.Pending, ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Shortlisted)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Accepted)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Shortlisted, ApplicationStatus.Accepted)]
    [InlineData(ApplicationStatus.Shortlisted, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Pending, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Shortlisted, ApplicationStatus.Withdrawn)]
    public void IsTransitionAllowed_Should_Return_True_For_Valid_Transitions(
        ApplicationStatus from, ApplicationStatus to)
    {
        ApplicationStateMachine.IsTransitionAllowed(from, to).Should().BeTrue();
    }

    // ── Disallowed transitions ────────────────────────────────────────────────

    [Theory]
    [InlineData(ApplicationStatus.Accepted, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Withdrawn, ApplicationStatus.Pending)]
    [InlineData(ApplicationStatus.Accepted, ApplicationStatus.Pending)]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Accepted)]
    [InlineData(ApplicationStatus.Pending, ApplicationStatus.Accepted)]
    public void IsTransitionAllowed_Should_Return_False_For_Invalid_Transitions(
        ApplicationStatus from, ApplicationStatus to)
    {
        ApplicationStateMachine.IsTransitionAllowed(from, to).Should().BeFalse();
    }

    [Fact]
    public void EnsureTransition_Should_Throw_ConflictException_For_Invalid_Transition()
    {
        var act = () => ApplicationStateMachine.EnsureTransition(
            ApplicationStatus.Accepted,
            ApplicationStatus.Withdrawn);

        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void EnsureTransition_Should_Not_Throw_For_Valid_Transition()
    {
        var act = () => ApplicationStateMachine.EnsureTransition(
            ApplicationStatus.Draft,
            ApplicationStatus.Pending);

        act.Should().NotThrow();
    }
}

// ─────────────────────────────────────────────────────────────────────────────

public class ApplicationTrackerIsActiveTests
{
    // ── Active statuses ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(ApplicationStatus.Draft)]
    [InlineData(ApplicationStatus.Pending)]
    [InlineData(ApplicationStatus.UnderReview)]
    public void IsActive_Should_Be_True_For_Non_Terminal_Statuses(
        ApplicationStatus status)
    {
        var app = new ApplicationTracker { Status = status };
        app.IsActive.Should().BeTrue();
    }

    // ── Terminal statuses ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Accepted)]
    [InlineData(ApplicationStatus.Shortlisted)]
    public void IsActive_Should_Be_False_For_Terminal_Statuses(
        ApplicationStatus status)
    {
        var app = new ApplicationTracker { Status = status };
        app.IsActive.Should().BeFalse();
    }

    // ── IsReadOnly ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ApplicationStatus.Accepted)]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public void IsReadOnly_Should_Be_True_For_Final_Decision_Statuses(
        ApplicationStatus status)
    {
        var app = new ApplicationTracker { Status = status };
        app.IsReadOnly.Should().BeTrue();
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft)]
    [InlineData(ApplicationStatus.Pending)]
    [InlineData(ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.Shortlisted)]
    public void IsReadOnly_Should_Be_False_For_Non_Final_Statuses(
        ApplicationStatus status)
    {
        var app = new ApplicationTracker { Status = status };
        app.IsReadOnly.Should().BeFalse();
    }
}
