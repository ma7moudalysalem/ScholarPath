using FluentValidation.TestHelper;
using ScholarPath.Application.Profile.Commands.ChangePassword;
using Xunit;

namespace ScholarPath.UnitTests.Profile;

/// <summary>
/// Tightened change-password policy (CR-PROF-05) — every flow (register / reset
/// / change) must require upper, lower, digit, special, 8–128 chars, and a
/// new password different from the current one.
/// </summary>
public sealed class ChangePasswordCommandValidatorTests
{
    private static ChangePasswordCommandValidator NewValidator() => new();

    [Fact]
    public void Password_without_special_character_is_rejected()
    {
        var cmd = new ChangePasswordCommand("Current123!", "OnlyLetters1");
        var result = NewValidator().TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Password_without_uppercase_is_rejected()
    {
        var cmd = new ChangePasswordCommand("Current123!", "lower123!");
        var result = NewValidator().TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Password_without_lowercase_is_rejected()
    {
        var cmd = new ChangePasswordCommand("Current123!", "UPPER123!");
        var result = NewValidator().TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Password_without_digit_is_rejected()
    {
        var cmd = new ChangePasswordCommand("Current123!", "NoDigits!");
        var result = NewValidator().TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Password_shorter_than_eight_chars_is_rejected()
    {
        var cmd = new ChangePasswordCommand("Current123!", "Aa1!");
        var result = NewValidator().TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Password_longer_than_128_chars_is_rejected()
    {
        var cmd = new ChangePasswordCommand("Current123!", new string('A', 130) + "1!a");
        var result = NewValidator().TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Password_equal_to_current_is_rejected()
    {
        const string same = "SamePass1!";
        var cmd = new ChangePasswordCommand(same, same);
        var result = NewValidator().TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Password_meeting_all_rules_is_accepted()
    {
        var cmd = new ChangePasswordCommand("Current123!", "FreshOne$2026");
        var result = NewValidator().TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Missing_current_password_is_rejected()
    {
        var cmd = new ChangePasswordCommand("", "FreshOne$2026");
        var result = NewValidator().TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }
}
