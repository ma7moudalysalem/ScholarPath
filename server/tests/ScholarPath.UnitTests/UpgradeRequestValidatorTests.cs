using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Auth.Validators;

namespace ScholarPath.UnitTests;

public class UpgradeRequestValidatorTests
{
    // CompanyUpgradeRequestValidator 

    private readonly CompanyUpgradeRequestValidator _companyValidator = new();
    private readonly ConsultantUpgradeRequestValidator _consultantValidator = new();

    // Valid company request 
    [Fact]
    public void Company_validator_passes_for_valid_request()
    {
        var request = new CompanyUpgradeRequest(
            CompanyName: "Acme Corp",
            CompanyCountry: "Egypt",
            CompanyWebsite: "https://acme.com",
            ContactPersonName: "John Doe",
            ContactEmail: "john@acme.com",
            ContactPhone: "+201234567890",
            CompanyRegistrationNumber: "CRN-1234",
            ProofDocumentUrl: null
        );

        var result = _companyValidator.Validate(request);

        Assert.True(result.IsValid);
    }

    // CRN validation 
    [Theory]
    [InlineData("CRN-1234", true)]       // valid
    [InlineData("ABC123", true)]          // valid
    [InlineData("A1-B2", true)]           // valid with hyphen
    [InlineData("AB", false)]             // too short (< 4)
    [InlineData("CRN 123", false)]        // space not allowed
    [InlineData("CRN@123", false)]        // special char not allowed
    [InlineData("A123456789012345678901234567890", false)] // too long (> 30)
    public void Company_validator_CRN_validation(string crn, bool expectedValid)
    {
        var request = new CompanyUpgradeRequest(
            CompanyName: "Acme Corp",
            CompanyCountry: "Egypt",
            CompanyWebsite: null,
            ContactPersonName: "John Doe",
            ContactEmail: "john@acme.com",
            ContactPhone: null,
            CompanyRegistrationNumber: crn,
            ProofDocumentUrl: null
        );

        var result = _companyValidator.Validate(request);

        Assert.Equal(expectedValid, result.IsValid);
    }

    // Business Required fields 
    [Fact]
    public void Company_validator_fails_when_company_name_is_empty()
    {
        var request = new CompanyUpgradeRequest(
            CompanyName: "",
            CompanyCountry: "Egypt",
            CompanyWebsite: null,
            ContactPersonName: "John Doe",
            ContactEmail: "john@acme.com",
            ContactPhone: null,
            CompanyRegistrationNumber: "CRN-1234",
            ProofDocumentUrl: null
        );

        var result = _companyValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "errors.validation.companyNameRequired");
    }

    [Fact]
    public void Company_validator_fails_when_contact_email_is_invalid()
    {
        var request = new CompanyUpgradeRequest(
            CompanyName: "Acme Corp",
            CompanyCountry: "Egypt",
            CompanyWebsite: null,
            ContactPersonName: "John Doe",
            ContactEmail: "not-an-email",
            ContactPhone: null,
            CompanyRegistrationNumber: "CRN-1234",
            ProofDocumentUrl: null
        );

        var result = _companyValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "errors.validation.contactEmailInvalid");
    }

    [Fact]
    public void Company_validator_fails_when_crn_is_empty()
    {
        var request = new CompanyUpgradeRequest(
            CompanyName: "Acme Corp",
            CompanyCountry: "Egypt",
            CompanyWebsite: null,
            ContactPersonName: "John Doe",
            ContactEmail: "john@acme.com",
            ContactPhone: null,
            CompanyRegistrationNumber: "",
            ProofDocumentUrl: null
        );

        var result = _companyValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "errors.validation.crnRequired");
    }

    [Fact]
    public void Company_validator_fails_when_website_url_is_invalid()
    {
        var request = new CompanyUpgradeRequest(
            CompanyName: "Acme Corp",
            CompanyCountry: "Egypt",
            CompanyWebsite: "not-a-url",
            ContactPersonName: "John Doe",
            ContactEmail: "john@acme.com",
            ContactPhone: null,
            CompanyRegistrationNumber: "CRN-1234",
            ProofDocumentUrl: null
        );

        var result = _companyValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "errors.validation.companyWebsiteInvalid");
    }

    [Fact]
    public void Company_validator_passes_when_optional_fields_are_null()
    {
        var request = new CompanyUpgradeRequest(
            CompanyName: "Acme Corp",
            CompanyCountry: "Egypt",
            CompanyWebsite: null,
            ContactPersonName: "John Doe",
            ContactEmail: "john@acme.com",
            ContactPhone: null,
            CompanyRegistrationNumber: "CRN-1234",
            ProofDocumentUrl: null
        );

        var result = _companyValidator.Validate(request);

        Assert.True(result.IsValid);
    }

    // ConsultantUpgradeRequestValidator 

    [Fact]
    public void Consultant_validator_passes_for_valid_request()
    {
        var request = new ConsultantUpgradeRequest(
            ExperienceSummary: "10 years in scholarship guidance",
            ExpertiseTags: "Fulbright, Chevening",
            Languages: "English, Arabic",
            LinkedInUrl: "https://linkedin.com/in/johndoe",
            PortfolioUrl: null,
            ProofDocumentUrl: null
        );

        var result = _consultantValidator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Consultant_validator_fails_when_experience_summary_is_empty()
    {
        var request = new ConsultantUpgradeRequest(
            ExperienceSummary: "",
            ExpertiseTags: "Fulbright",
            Languages: "English",
            LinkedInUrl: null,
            PortfolioUrl: null,
            ProofDocumentUrl: null
        );

        var result = _consultantValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "errors.validation.experienceSummaryRequired");
    }

    [Fact]
    public void Consultant_validator_fails_when_linkedin_url_is_invalid()
    {
        var request = new ConsultantUpgradeRequest(
            ExperienceSummary: "10 years experience",
            ExpertiseTags: "Fulbright",
            Languages: "English",
            LinkedInUrl: "not-a-url",
            PortfolioUrl: null,
            ProofDocumentUrl: null
        );

        var result = _consultantValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "errors.validation.linkedInUrlInvalid");
    }

    [Fact]
    public void Consultant_validator_passes_when_optional_urls_are_null()
    {
        var request = new ConsultantUpgradeRequest(
            ExperienceSummary: "10 years experience",
            ExpertiseTags: "Fulbright",
            Languages: "English",
            LinkedInUrl: null,
            PortfolioUrl: null,
            ProofDocumentUrl: null
        );

        var result = _consultantValidator.Validate(request);

        Assert.True(result.IsValid);
    }
}


































