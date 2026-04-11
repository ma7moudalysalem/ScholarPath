using FluentAssertions;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.UnitTests.Smoke;

/// <summary>
/// Baseline smoke tests so the CI test runner has at least one assertion to execute.
/// Teammates: replace this class with real tests per your module spec.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void ApplicationUser_Defaults_ShouldBeUnassigned()
    {
        var user = new ApplicationUser { FirstName = "Alaa", LastName = "Mostafa" };

        user.AccountStatus.Should().Be(AccountStatus.Unassigned);
        user.IsOnboardingComplete.Should().BeFalse();
        user.FullName.Should().Be("Alaa Mostafa");
    }

    [Fact]
    public void Scholarship_NewInstance_ShouldDefaultToDraft()
    {
        var scholarship = new Scholarship
        {
            TitleEn = "Fully-Funded Masters",
            TitleAr = "منحة ماجستير ممولة بالكامل",
            DescriptionEn = "Full tuition + stipend",
            DescriptionAr = "رسوم + مكافأة",
            Slug = "fully-funded-masters",
            Deadline = DateTimeOffset.UtcNow.AddMonths(3),
        };

        scholarship.Status.Should().Be(ScholarshipStatus.Draft);
        scholarship.Mode.Should().Be(ListingMode.InApp);
        scholarship.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void ApplicationTracker_NewInstance_ShouldBeActive()
    {
        var tracker = new ApplicationTracker
        {
            StudentId = Guid.NewGuid(),
            ScholarshipId = Guid.NewGuid(),
        };

        tracker.Status.Should().Be(ApplicationStatus.Draft);
        tracker.IsActive.Should().BeTrue();
        tracker.IsReadOnly.Should().BeFalse();
    }

    [Theory]
    [InlineData(ApplicationStatus.Accepted, false)]
    [InlineData(ApplicationStatus.Rejected, false)]
    [InlineData(ApplicationStatus.Withdrawn, false)]
    [InlineData(ApplicationStatus.Pending, true)]
    [InlineData(ApplicationStatus.UnderReview, true)]
    public void ApplicationTracker_IsActive_ShouldReflectStatus(ApplicationStatus status, bool expected)
    {
        var tracker = new ApplicationTracker
        {
            StudentId = Guid.NewGuid(),
            ScholarshipId = Guid.NewGuid(),
            Status = status,
        };

        tracker.IsActive.Should().Be(expected);
    }
}
