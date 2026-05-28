using FluentAssertions;
using ScholarPath.Application.Scholarships.Commands;
using ScholarPath.Domain.Enums;
using Xunit;

namespace ScholarPath.UnitTests.Scholarships;

/// <summary>
/// PB-005: the Review Service Fee is required for in-app scholarship listings
/// so the Apply Now flow always has a price to authorise. External-URL
/// listings settle off-platform and are exempt.
/// </summary>
public class CreateScholarshipReviewFeeValidatorTests
{
    private static CreateScholarshipCommand Base(
        decimal? reviewFeeUsd, ListingMode mode = ListingMode.InApp) =>
        new()
        {
            TitleEn = "T", TitleAr = "ع",
            DescriptionEn = "D", DescriptionAr = "وصف",
            CategoryId = Guid.NewGuid(),
            Deadline = DateTimeOffset.UtcNow.AddDays(30),
            FundingType = FundingType.FullyFunded,
            TargetLevel = AcademicLevel.Undergrad,
            Mode = mode,
            ExternalApplicationUrl = mode == ListingMode.ExternalUrl
                ? "https://example.com/apply" : null,
            ReviewFeeUsd = reviewFeeUsd,
        };

    [Fact]
    public void Rejects_in_app_listing_with_null_fee()
    {
        var v = new CreateScholarshipCommandValidator();
        v.Validate(Base(null)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Accepts_in_app_listing_with_zero_fee()
    {
        // A fee of 0 marks the listing as free for the Student (no payment
        // authorisation, no commission). The validator must allow this.
        var v = new CreateScholarshipCommandValidator();
        v.Validate(Base(0m)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_in_app_listing_with_negative_fee()
    {
        var v = new CreateScholarshipCommandValidator();
        v.Validate(Base(-1m)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_in_app_listing_with_fee_above_500()
    {
        var v = new CreateScholarshipCommandValidator();
        v.Validate(Base(501m)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Accepts_in_app_listing_with_positive_fee()
    {
        var v = new CreateScholarshipCommandValidator();
        v.Validate(Base(150m)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void External_url_listing_does_not_require_fee()
    {
        // External listings let the student leave the platform to apply; the
        // company is paid off-platform, so the fee column stays null.
        var v = new CreateScholarshipCommandValidator();
        v.Validate(Base(null, ListingMode.ExternalUrl)).IsValid.Should().BeTrue();
    }
}
