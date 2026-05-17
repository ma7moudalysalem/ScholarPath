using ScholarPath.Application.Auth.Commands.ConfirmEmailChange;
using ScholarPath.Application.Auth.Commands.RequestEmailChange;
using Xunit;
using FluentAssertions;

namespace ScholarPath.UnitTests.Auth;

/// <summary>SRS FR-231 — change-email request / confirm command validation.</summary>
public class RequestEmailChangeValidatorTests
{
    private readonly RequestEmailChangeCommandValidator _v = new();

    [Fact]
    public void Valid_email_passes()
        => _v.Validate(new RequestEmailChangeCommand("new@example.com")).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Bad_email_fails(string email)
        => _v.Validate(new RequestEmailChangeCommand(email)).IsValid.Should().BeFalse();
}

public class ConfirmEmailChangeValidatorTests
{
    private readonly ConfirmEmailChangeCommandValidator _v = new();

    [Fact]
    public void Valid_request_passes()
        => _v.Validate(new ConfirmEmailChangeCommand("new@example.com", "tok")).IsValid.Should().BeTrue();

    [Fact]
    public void Missing_token_fails()
        => _v.Validate(new ConfirmEmailChangeCommand("new@example.com", "")).IsValid.Should().BeFalse();

    [Fact]
    public void Bad_email_fails()
        => _v.Validate(new ConfirmEmailChangeCommand("nope", "tok")).IsValid.Should().BeFalse();
}
