using ScholarPath.Application.Admin.Commands.ReviewUpgradeRequest;

namespace ScholarPath.UnitTests.Admin;

public class ReviewUpgradeRequestValidatorTests
{
    private readonly ReviewUpgradeRequestCommandValidator _v = new();

    [Fact]
    public void Approve_without_notes_is_valid()
    {
        var r = _v.Validate(new ReviewUpgradeRequestCommand(Guid.NewGuid(), UpgradeDecision.Approve, null));
        r.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Reject_requires_notes()
    {
        var r = _v.Validate(new ReviewUpgradeRequestCommand(Guid.NewGuid(), UpgradeDecision.Reject, null));
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == nameof(ReviewUpgradeRequestCommand.ReviewerNotes));
    }

    [Fact]
    public void Empty_request_id_is_rejected()
    {
        var r = _v.Validate(new ReviewUpgradeRequestCommand(Guid.Empty, UpgradeDecision.Approve, null));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Notes_over_1000_chars_fail()
    {
        var longNotes = new string('x', 1001);
        var r = _v.Validate(new ReviewUpgradeRequestCommand(Guid.NewGuid(), UpgradeDecision.Reject, longNotes));
        r.IsValid.Should().BeFalse();
    }
}
