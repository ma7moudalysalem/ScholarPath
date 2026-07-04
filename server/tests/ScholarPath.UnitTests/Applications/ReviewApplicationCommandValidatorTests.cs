using FluentAssertions;
using ScholarPath.Application.Applications.Commands.ReviewApplication;
using ScholarPath.Domain.Enums;
using Xunit;

namespace ScholarPath.UnitTests.Applications;

// BUG-02: the ReviewApplication validator must admit every status that is a
// legitimate provider review decision — previously it rejected the valid
// Shortlisted (and UnderReview) with a 422.
public class ReviewApplicationCommandValidatorTests
{
    private readonly ReviewApplicationCommandValidator _v = new();

    [Theory]
    [InlineData(ApplicationStatus.Shortlisted)]
    [InlineData(ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.Accepted)]
    public void Valid_review_statuses_pass(ApplicationStatus status)
    {
        // Non-reject decisions don't require a reason.
        _v.Validate(new ReviewApplicationCommand(Guid.NewGuid(), status, null))
            .IsValid.Should().BeTrue();
    }

    // FR-APP-30: rejecting an application requires a rejection reason.
    [Fact]
    public void Reject_without_reason_is_rejected()
    {
        _v.Validate(new ReviewApplicationCommand(Guid.NewGuid(), ApplicationStatus.Rejected, null))
            .IsValid.Should().BeFalse();

        _v.Validate(new ReviewApplicationCommand(Guid.NewGuid(), ApplicationStatus.Rejected, "   "))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Reject_with_reason_passes()
    {
        _v.Validate(new ReviewApplicationCommand(Guid.NewGuid(), ApplicationStatus.Rejected, "Incomplete transcript."))
            .IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft)]
    [InlineData(ApplicationStatus.Pending)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public void Non_decision_statuses_fail(ApplicationStatus status)
    {
        _v.Validate(new ReviewApplicationCommand(Guid.NewGuid(), status, null))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_application_id_is_rejected()
    {
        _v.Validate(new ReviewApplicationCommand(Guid.Empty, ApplicationStatus.Accepted, null))
            .IsValid.Should().BeFalse();
    }
}
