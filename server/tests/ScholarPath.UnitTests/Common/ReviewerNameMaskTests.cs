using FluentAssertions;
using ScholarPath.Application.Common;
using Xunit;

namespace ScholarPath.UnitTests.Common;

public sealed class ReviewerNameMaskTests
{
    [Theory]
    [InlineData("Sarah", "Adams", "Sarah A.")]
    [InlineData("Mohamed", "Aly Salem", "Mohamed A. S.")]
    [InlineData("Omar", "", "Omar")]
    [InlineData("Omar", null, "Omar")]
    public void Mask_FirstLast_KeepsGivenNameAndInitialsTheRest(
        string? first, string? last, string expected)
        => ReviewerNameMask.Mask(first, last).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Mask_BlankFullName_FallsBackToAnonymous(string? fullName)
        => ReviewerNameMask.Mask(fullName).Should().Be("Anonymous");

    [Fact]
    public void Mask_SingleName_ReturnedAsIs()
        => ReviewerNameMask.Mask("Cher").Should().Be("Cher");

    [Fact]
    public void Mask_CollapsesExtraWhitespaceBetweenParts()
        => ReviewerNameMask.Mask("  Sarah   Adams  ").Should().Be("Sarah A.");

    [Fact]
    public void Mask_LowercaseInitial_IsUppercased()
        => ReviewerNameMask.Mask("sarah", "adams").Should().Be("sarah A.");
}
