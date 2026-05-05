using ScholarPath.Application.UpgradeRequests.DTOs;
using ScholarPath.Application.UpgradeRequests.Validators;

namespace ScholarPath.UnitTests;

public class UpgradeRequestValidatorTests
{
    private readonly ConsultantUpgradeRequestValidator _consultantValidator = new();
    private readonly CompanyUpgradeRequestValidator _companyValidator = new();

    private static ConsultantUpgradeRequest ValidConsultantRequest() => new(
        Education: new List<EducationEntryDto>
        {
            new("MIT", "BSc", "Computer Science", 2018, 2022, false)
        },
        ExperienceSummary: new string('x', 50),
        ExpertiseTags: new List<string> { "Admissions", "Guidance" },
        Languages: new List<string> { "English", "Arabic" },
        Links: new List<UpgradeRequestLinkDto>
        {
            new("https://linkedin.com/in/test", "LinkedIn")
        });

    private static CompanyUpgradeRequest ValidCompanyRequest() => new(
        CompanyName: "Acme Corp",
        Country: "Egypt",
        Website: "https://acme.com",
        ContactPersonName: "John Doe",
        ContactEmail: "john@acme.com",
        ContactPhone: "+201234567890",
        CompanyRegistrationNumber: "CRN-12345");

    [Fact]
    public void Consultant_valid_request_passes()
    {
        var result = _consultantValidator.Validate(ValidConsultantRequest());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Consultant_empty_education_fails()
    {
        var request = ValidConsultantRequest() with { Education = new List<EducationEntryDto>() };
        var result = _consultantValidator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Consultant_short_experience_summary_fails()
    {
        var request = ValidConsultantRequest() with { ExperienceSummary = "short" };
        var result = _consultantValidator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Consultant_too_many_expertise_tags_fails()
    {
        var tags = Enumerable.Range(1, 11).Select(i => $"Tag{i}").ToList();
        var request = ValidConsultantRequest() with { ExpertiseTags = tags };
        var result = _consultantValidator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Consultant_too_many_languages_fails()
    {
        var langs = Enumerable.Range(1, 6).Select(i => $"Lang{i}").ToList();
        var request = ValidConsultantRequest() with { Languages = langs };
        var result = _consultantValidator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Consultant_too_many_links_fails()
    {
        var links = Enumerable.Range(1, 4).Select(i => new UpgradeRequestLinkDto($"https://example{i}.com", "Other")).ToList();
        var request = ValidConsultantRequest() with { Links = links };
        var result = _consultantValidator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Company_valid_request_passes()
    {
        var result = _companyValidator.Validate(ValidCompanyRequest());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Company_missing_crn_fails()
    {
        var request = ValidCompanyRequest() with { CompanyRegistrationNumber = "" };
        var result = _companyValidator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Company_invalid_crn_format_fails()
    {
        var request = ValidCompanyRequest() with { CompanyRegistrationNumber = "CRN @#!" };
        var result = _companyValidator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Company_short_crn_fails()
    {
        var request = ValidCompanyRequest() with { CompanyRegistrationNumber = "AB" };
        var result = _companyValidator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Company_missing_contact_email_fails()
    {
        var request = ValidCompanyRequest() with { ContactEmail = "" };
        var result = _companyValidator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Company_invalid_contact_email_fails()
    {
        var request = ValidCompanyRequest() with { ContactEmail = "not-an-email" };
        var result = _companyValidator.Validate(request);
        Assert.False(result.IsValid);
    }
}
