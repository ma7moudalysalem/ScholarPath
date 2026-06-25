using ScholarPath.Infrastructure.Services;
using Xunit;
using FluentAssertions;

namespace ScholarPath.UnitTests.Common;

/// <summary>
/// Country values reach the matcher in three shapes: ISO alpha-2 codes (how
/// scholarships store them, e.g. "EG"), full English names (the profile
/// nationality selector, "Egypt"), and free-typed text including Arabic ("مصر").
/// <see cref="CountryNormalizer"/> must fold all three to one key.
/// </summary>
public sealed class CountryNormalizerTests
{
    [Theory]
    [InlineData("EG", "Egypt")]
    [InlineData("eg", "egypt")]
    [InlineData("Egypt", "مصر")]
    [InlineData("EG", "مصر")]
    [InlineData("United States", "US")]
    [InlineData("USA", "us")]
    [InlineData("UK", "United Kingdom")]   // "UK" is an alias, not its own ISO code (GB)
    [InlineData("United Arab Emirates", "AE")]
    [InlineData("  Egypt  ", "EG")]
    public void Matches_EquivalentForms_AreEqual(string a, string b)
        => CountryNormalizer.Matches(a, b).Should().BeTrue();

    [Theory]
    [InlineData("EG", "DE")]
    [InlineData("Egypt", "Germany")]
    [InlineData("US", "GB")]
    [InlineData("UK", "US")]
    public void Matches_DifferentCountries_AreNotEqual(string a, string b)
        => CountryNormalizer.Matches(a, b).Should().BeFalse();

    [Theory]
    [InlineData("Egypt", "EG")]
    [InlineData("مصر", "EG")]
    [InlineData("USA", "US")]
    [InlineData("UK", "GB")]
    [InlineData("united kingdom", "GB")]
    public void ToKey_ResolvesToIsoCode(string input, string expected)
        => CountryNormalizer.ToKey(input).Should().Be(expected);

    [Fact]
    public void ToKey_BlankOrNull_ReturnsEmpty()
    {
        CountryNormalizer.ToKey(null).Should().BeEmpty();
        CountryNormalizer.ToKey("   ").Should().BeEmpty();
    }

    [Fact]
    public void Matches_BothBlank_IsFalse()
        => CountryNormalizer.Matches("", "").Should().BeFalse();
}
