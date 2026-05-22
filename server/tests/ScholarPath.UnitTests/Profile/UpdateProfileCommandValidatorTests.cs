using FluentAssertions;
using FluentValidation.TestHelper;
using NSubstitute;
using ScholarPath.Application.Profile.Commands.UpdateProfile;
using ScholarPath.Application.Profile.DTOs;
using ScholarPath.Domain.Interfaces;
using Xunit;

namespace ScholarPath.UnitTests.Profile;

/// <summary>
/// Backend rules for the profile PATCH endpoint (CR-PROF-01..04, CR-PROF-08,
/// CR-PROF-09). Each test covers a single rule so a regression points straight
/// at the broken behaviour.
/// </summary>
public sealed class UpdateProfileCommandValidatorTests
{
    private static UpdateProfileCommandValidator NewValidator(DateOnly? today = null)
    {
        var clock = Substitute.For<IDateTimeService>();
        clock.Today.Returns(today ?? new DateOnly(2026, 5, 22));
        return new UpdateProfileCommandValidator(clock);
    }

    private static UpdateProfileCommand Cmd(UpdateProfileRequestDto fields) => new(fields);

    private static UpdateProfileRequestDto Empty() => new(
        FirstName: null, LastName: null, CountryOfResidence: null, PreferredLanguage: null,
        Biography: null, DateOfBirth: null, Nationality: null, LinkedInUrl: null, WebsiteUrl: null,
        AcademicLevel: null, FieldOfStudy: null, CurrentInstitution: null, Gpa: null, GpaScale: null,
        OrganizationLegalName: null, OrganizationWebsite: null, SessionFeeUsd: null,
        SessionDurationMinutes: null, ProfessionalTitle: null, YearsOfExperience: null,
        ExpertiseTags: null, Languages: null, Timezone: null,
        PreferredCountries: null, PreferredFields: null);

    // ── GPA / GpaScale (CR-PROF-02) ──────────────────────────────────────────

    [Theory]
    [InlineData("4.0", 3.7)]
    [InlineData("5.0", 4.5)]
    [InlineData("10.0", 9.4)]
    [InlineData("20.0", 19.2)]
    [InlineData("100", 88)]
    public void Gpa_within_scale_is_accepted(string scale, double gpa)
    {
        var fields = Empty() with { Gpa = (decimal)gpa, GpaScale = scale };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldNotHaveValidationErrorFor(x => x.Fields.Gpa);
        result.ShouldNotHaveValidationErrorFor(x => x.Fields.GpaScale);
    }

    [Theory]
    [InlineData("5.0", 6.0)]   // over scale
    [InlineData("4.0", 4.5)]   // over scale
    [InlineData("100", 110)]   // over scale
    [InlineData("4.0", -0.1)]  // below 0
    public void Gpa_outside_scale_is_rejected(string scale, double gpa)
    {
        var fields = Empty() with { Gpa = (decimal)gpa, GpaScale = scale };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldHaveValidationErrorFor(x => x.Fields.Gpa);
    }

    [Fact]
    public void Gpa_without_scale_is_rejected()
    {
        var fields = Empty() with { Gpa = 3.4m, GpaScale = null };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldHaveValidationErrorFor(x => x.Fields.GpaScale);
    }

    [Theory]
    [InlineData("6.0")]
    [InlineData("3.0")]
    [InlineData("notascale")]
    public void Unsupported_gpa_scale_is_rejected(string scale)
    {
        var fields = Empty() with { GpaScale = scale };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldHaveValidationErrorFor(x => x.Fields.GpaScale);
    }

    // ── URLs (CR-PROF-03) ────────────────────────────────────────────────────

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com")]
    [InlineData("linkedin.com/in/me")]
    public void Invalid_linkedin_url_is_rejected(string url)
    {
        var fields = Empty() with { LinkedInUrl = url };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldHaveValidationErrorFor(x => x.Fields.LinkedInUrl);
    }

    [Theory]
    [InlineData("https://linkedin.com/in/me")]
    [InlineData("http://example.org/profile")]
    public void Valid_http_url_is_accepted(string url)
    {
        var fields = Empty() with { LinkedInUrl = url, WebsiteUrl = url, OrganizationWebsite = url };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldNotHaveValidationErrorFor(x => x.Fields.LinkedInUrl);
        result.ShouldNotHaveValidationErrorFor(x => x.Fields.WebsiteUrl);
        result.ShouldNotHaveValidationErrorFor(x => x.Fields.OrganizationWebsite);
    }

    [Fact]
    public void Empty_url_is_accepted_as_clearing_the_field()
    {
        var fields = Empty() with { LinkedInUrl = "", WebsiteUrl = "" };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldNotHaveValidationErrorFor(x => x.Fields.LinkedInUrl);
        result.ShouldNotHaveValidationErrorFor(x => x.Fields.WebsiteUrl);
    }

    // ── Date of birth (CR-PROF-04) ───────────────────────────────────────────

    [Fact]
    public void Future_date_of_birth_is_rejected()
    {
        var today = new DateOnly(2026, 5, 22);
        var fields = Empty() with { DateOfBirth = today.AddDays(1) };
        var result = NewValidator(today).TestValidate(Cmd(fields));
        result.ShouldHaveValidationErrorFor(x => x.Fields.DateOfBirth);
    }

    [Fact]
    public void Past_or_today_date_of_birth_is_accepted()
    {
        var today = new DateOnly(2026, 5, 22);
        var fields = Empty() with { DateOfBirth = today };
        NewValidator(today).TestValidate(Cmd(fields))
            .ShouldNotHaveValidationErrorFor(x => x.Fields.DateOfBirth);

        var oldEnough = Empty() with { DateOfBirth = new DateOnly(1990, 1, 1) };
        NewValidator(today).TestValidate(Cmd(oldEnough))
            .ShouldNotHaveValidationErrorFor(x => x.Fields.DateOfBirth);
    }

    // ── Names ────────────────────────────────────────────────────────────────

    [Fact]
    public void Blank_first_or_last_name_is_rejected_when_present()
    {
        var fields = Empty() with { FirstName = "  ", LastName = "" };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldHaveValidationErrorFor(x => x.Fields.FirstName);
        result.ShouldHaveValidationErrorFor(x => x.Fields.LastName);
    }

    // ── Consultant session settings (CR-PROF-09) ─────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Non_positive_session_fee_is_rejected(double fee)
    {
        var fields = Empty() with { SessionFeeUsd = (decimal)fee };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldHaveValidationErrorFor(x => x.Fields.SessionFeeUsd);
    }

    [Fact]
    public void Session_fee_with_more_than_two_decimal_places_is_rejected()
    {
        var fields = Empty() with { SessionFeeUsd = 10.999m };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldHaveValidationErrorFor(x => x.Fields.SessionFeeUsd);
    }

    [Fact]
    public void Session_fee_with_two_decimal_places_is_accepted()
    {
        var fields = Empty() with { SessionFeeUsd = 10.99m };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldNotHaveValidationErrorFor(x => x.Fields.SessionFeeUsd);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(20)]
    [InlineData(180)]
    public void Disallowed_session_duration_is_rejected(int minutes)
    {
        var fields = Empty() with { SessionDurationMinutes = minutes };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldHaveValidationErrorFor(x => x.Fields.SessionDurationMinutes);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(45)]
    [InlineData(60)]
    [InlineData(90)]
    [InlineData(120)]
    public void Allowed_session_duration_is_accepted(int minutes)
    {
        var fields = Empty() with { SessionDurationMinutes = minutes };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldNotHaveValidationErrorFor(x => x.Fields.SessionDurationMinutes);
    }

    // ── Consultant professional fields (CR-PROF-08) ──────────────────────────

    [Fact]
    public void Empty_expertise_tags_list_is_rejected()
    {
        var fields = Empty() with { ExpertiseTags = Array.Empty<string>() };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldHaveValidationErrorFor(x => x.Fields.ExpertiseTags);
    }

    [Fact]
    public void Single_expertise_tag_is_accepted()
    {
        var fields = Empty() with { ExpertiseTags = new[] { "PhD applications" } };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldNotHaveValidationErrorFor(x => x.Fields.ExpertiseTags);
    }

    [Fact]
    public void Years_of_experience_outside_range_is_rejected()
    {
        var negative = Empty() with { YearsOfExperience = -1 };
        NewValidator().TestValidate(Cmd(negative))
            .ShouldHaveValidationErrorFor(x => x.Fields.YearsOfExperience);

        var huge = Empty() with { YearsOfExperience = 200 };
        NewValidator().TestValidate(Cmd(huge))
            .ShouldHaveValidationErrorFor(x => x.Fields.YearsOfExperience);
    }

    [Fact]
    public void Unknown_timezone_is_rejected()
    {
        var fields = Empty() with { Timezone = "Atlantis/Atlantica" };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldHaveValidationErrorFor(x => x.Fields.Timezone);
    }

    [Theory]
    [InlineData("UTC")]
    [InlineData("Europe/London")]
    public void Recognised_timezone_is_accepted(string tz)
    {
        var fields = Empty() with { Timezone = tz };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldNotHaveValidationErrorFor(x => x.Fields.Timezone);
    }

    // ── Organization (CR-PROF-07) ────────────────────────────────────────────

    [Fact]
    public void Blank_organization_legal_name_is_rejected_when_present()
    {
        var fields = Empty() with { OrganizationLegalName = " " };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldHaveValidationErrorFor(x => x.Fields.OrganizationLegalName);
    }

    // ── Preferred language ───────────────────────────────────────────────────

    [Theory]
    [InlineData("en")]
    [InlineData("ar")]
    public void Supported_preferred_languages_are_accepted(string lang)
    {
        var fields = Empty() with { PreferredLanguage = lang };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldNotHaveValidationErrorFor(x => x.Fields.PreferredLanguage);
    }

    [Fact]
    public void Unsupported_preferred_language_is_rejected()
    {
        var fields = Empty() with { PreferredLanguage = "fr" };
        var result = NewValidator().TestValidate(Cmd(fields));
        result.ShouldHaveValidationErrorFor(x => x.Fields.PreferredLanguage);
    }

    // ── Mass-assignment defence (CR-PROF-11) ─────────────────────────────────

    [Fact]
    public void UpdateProfileRequestDto_has_no_role_or_status_fields()
    {
        // If a contributor accidentally adds a Role / ActiveRole / AccountStatus /
        // VerificationStatus / ApprovalStatus property to the DTO, this test
        // tells them to put it on a dedicated admin command instead.
        var propertyNames = typeof(UpdateProfileRequestDto)
            .GetProperties()
            .Select(p => p.Name)
            .ToArray();
        propertyNames.Should().NotContain([
            "Role",
            "ActiveRole",
            "AccountStatus",
            "VerificationStatus",
            "OrganizationVerificationStatus",
            "ApprovalStatus",
            "IsAdmin",
            "Roles",
        ]);
    }
}
