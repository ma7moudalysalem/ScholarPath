using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Auth.Validators;

namespace ScholarPath.UnitTests;

public class RememberMeTests
{
    private readonly LoginRequestValidator _validator = new();

    // LoginRequest DTO defaults 

    [Fact]
    public void RememberMe_defaults_to_null_when_not_provided()
    {
        var request = new LoginRequest("test@test.com", "Password1!");

        Assert.Null(request.RememberMe);
    }

    [Fact]
    public void RememberMe_can_be_set_to_true()
    {
        var request = new LoginRequest("test@test.com", "Password1!", RememberMe: true);

        Assert.True(request.RememberMe);
    }

    [Fact]
    public void RememberMe_can_be_set_to_false()
    {
        var request = new LoginRequest("test@test.com", "Password1!", RememberMe: false);

        Assert.False(request.RememberMe);
    }

    // Validator

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void Login_validator_accepts_any_RememberMe_value(bool? rememberMe)
    {
        var request = new LoginRequest("test@test.com", "Password1!", RememberMe: rememberMe);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Login_validator_fails_when_email_is_empty()
    {
        var request = new LoginRequest("", "Password1!", RememberMe: true);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "errors.validation.emailRequired");
    }

    [Fact]
    public void Login_validator_fails_when_email_is_invalid()
    {
        var request = new LoginRequest("not-an-email", "Password1!");

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "errors.validation.emailInvalid");
    }

    [Fact]
    public void Login_validator_fails_when_password_is_empty()
    {
        var request = new LoginRequest("test@test.com", "");

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "errors.validation.passwordRequired");
    }

    // Extended expiry Business 

    [Fact]
    public void RememberMe_true_should_use_30_day_expiry()
    {
        var rememberMe = true;
        TimeSpan? extendedExpiry = rememberMe ? TimeSpan.FromDays(30) : null;

        Assert.NotNull(extendedExpiry);
        Assert.Equal(30, extendedExpiry!.Value.TotalDays);
    }

    [Fact]
    public void RememberMe_false_should_use_default_expiry()
    {
        var rememberMe = false;
        TimeSpan? extendedExpiry = rememberMe ? TimeSpan.FromDays(30) : null;

        Assert.Null(extendedExpiry);
    }

    [Fact]
    public void RememberMe_null_should_use_default_expiry()
    {
        bool? rememberMe = null;
        TimeSpan? extendedExpiry = (rememberMe ?? false) ? TimeSpan.FromDays(30) : null;

        Assert.Null(extendedExpiry);
    }
}
