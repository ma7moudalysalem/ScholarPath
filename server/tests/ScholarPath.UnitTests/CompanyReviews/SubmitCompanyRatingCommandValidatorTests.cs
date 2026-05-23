using FluentAssertions;
using ScholarPath.Application.CompanyReviews.Commands.SubmitCompanyRating;
using Xunit;

namespace ScholarPath.UnitTests.CompanyReviews;

public sealed class SubmitCompanyRatingCommandValidatorTests
{
    private readonly SubmitCompanyRatingCommandValidator _v = new();

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Rating_in_range_is_valid(int rating)
    {
        _v.Validate(new SubmitCompanyRatingCommand(Guid.NewGuid(), rating, null))
            .IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(100)]
    public void Rating_out_of_range_is_invalid(int rating)
    {
        _v.Validate(new SubmitCompanyRatingCommand(Guid.NewGuid(), rating, null))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_application_id_is_invalid()
    {
        _v.Validate(new SubmitCompanyRatingCommand(Guid.Empty, 5, null))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Null_comment_is_allowed()
    {
        _v.Validate(new SubmitCompanyRatingCommand(Guid.NewGuid(), 5, null))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void Comment_at_max_length_is_allowed()
    {
        _v.Validate(new SubmitCompanyRatingCommand(Guid.NewGuid(), 5, new string('x', 1000)))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void Comment_over_max_length_is_invalid()
    {
        _v.Validate(new SubmitCompanyRatingCommand(Guid.NewGuid(), 5, new string('x', 1001)))
            .IsValid.Should().BeFalse();
    }
}
