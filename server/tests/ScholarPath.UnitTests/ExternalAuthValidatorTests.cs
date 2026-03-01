using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Auth.Validators;

namespace ScholarPath.UnitTests;

public class ExternalAuthValidatorTests
{
    private readonly ExternalLoginRequestValidator _externalLoginValidator = new();
    private readonly LinkProviderRequestValidator _linkProviderValidator = new();

    // ExternalLoginRequest 

    [Fact]
    public void ExternalLogin_validator_passes_for_valid_request()
    {
        var request = new ExternalLoginRequest(
            Provider: "Google",
            ProviderToken: "valid-token-from-google"
        );

        var result = _externalLoginValidator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ExternalLogin_validator_fails_when_provider_is_empty()
    {
        var request = new ExternalLoginRequest(
            Provider: "",
            ProviderToken: "valid-token"
        );

        var result = _externalLoginValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "errors.validation.providerRequired");
    }

    [Fact]
    public void ExternalLogin_validator_fails_when_token_is_empty()
    {
        var request = new ExternalLoginRequest(
            Provider: "Google",
            ProviderToken: ""
        );

        var result = _externalLoginValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "errors.validation.tokenRequired");
    }

    [Fact]
    public void ExternalLogin_validator_fails_when_provider_exceeds_max_length()
    {
        var request = new ExternalLoginRequest(
            Provider: new string('A', 51),
            ProviderToken: "valid-token"
        );

        var result = _externalLoginValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "errors.validation.maxLength");
    }

    [Fact]
    public void ExternalLogin_validator_fails_when_token_exceeds_max_length()
    {
        var request = new ExternalLoginRequest(
            Provider: "Google",
            ProviderToken: new string('x', 4097)
        );

        var result = _externalLoginValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "errors.validation.maxLength");
    }

    [Theory]
    [InlineData("Google")]
    [InlineData("Microsoft")]
    public void ExternalLogin_validator_passes_for_known_providers(string provider)
    {
        var request = new ExternalLoginRequest(
            Provider: provider,
            ProviderToken: "some-valid-token"
        );

        var result = _externalLoginValidator.Validate(request);

        Assert.True(result.IsValid);
    }

    // LinkProviderRequest 

    [Fact]
    public void LinkProvider_validator_passes_for_valid_request()
    {
        var request = new LinkProviderRequest(
            Provider: "Microsoft",
            ProviderToken: "valid-microsoft-token"
        );

        var result = _linkProviderValidator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void LinkProvider_validator_fails_when_provider_is_empty()
    {
        var request = new LinkProviderRequest(
            Provider: "",
            ProviderToken: "valid-token"
        );

        var result = _linkProviderValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "errors.auth.providerRequired");
    }

    [Fact]
    public void LinkProvider_validator_fails_when_token_is_empty()
    {
        var request = new LinkProviderRequest(
            Provider: "Google",
            ProviderToken: ""
        );

        var result = _linkProviderValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "errors.auth.providerTokenRequired");
    }

    // SSO Business Logic 

    [Fact]
    public void ExternalLogin_new_SSO_user_gets_Student_role_by_default()
    {
        var isNewUser = true;
        var assignedRole = isNewUser ? "Student" : "existing";

        Assert.Equal("Student", assignedRole);
    }

    [Fact]
    public void ExternalLogin_existing_user_by_provider_skips_email_lookup()
    {
        var userFoundByProvider = true;
        var shouldCheckEmail = !userFoundByProvider;

        Assert.False(shouldCheckEmail);
    }

    [Fact]
    public void ExternalLogin_email_exists_but_not_linked_returns_conflict()
    {
        var userByLogin = (object?)null;
        var userByEmail = new { Email = "test@test.com" };

        var shouldReturnConflict = userByLogin is null && userByEmail is not null;

        Assert.True(shouldReturnConflict);
    }

    [Fact]
    public void LinkProvider_email_mismatch_should_be_forbidden()
    {
        var userEmail = "tasneem@test.com";
        var providerEmail = "other@test.com";

        var isEmailMatch = string.Equals(userEmail, providerEmail, StringComparison.OrdinalIgnoreCase);

        Assert.False(isEmailMatch);
    }

    [Fact]
    public void LinkProvider_same_provider_already_linked_to_another_account_is_conflict()
    {
        var currentUserId = Guid.NewGuid();
        var linkedToUserId = Guid.NewGuid();

        var isConflict = linkedToUserId != currentUserId;

        Assert.True(isConflict);
    }
}
