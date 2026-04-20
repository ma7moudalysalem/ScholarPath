using ScholarPath.Application.Auth.Commands.ForgotPassword;
using ScholarPath.Application.Auth.Commands.ResetPassword;
using ScholarPath.Application.Auth.Commands.ChangePassword;

namespace ScholarPath.UnitTests;

public class PasswordCommandsTests
{
    [Fact]
    public void ForgotPasswordCommand_stores_email()
    {
        var command = new ForgotPasswordCommand("test@email.com");

        Assert.Equal("test@email.com", command.Email);
    }

    [Fact]
    public void ResetPasswordCommand_stores_token_and_password()
    {
        var command = new ResetPasswordCommand("reset-token", "NewPass123!");

        Assert.Equal("reset-token", command.Token);
        Assert.Equal("NewPass123!", command.NewPassword);
    }

    [Fact]
    public void ChangePasswordCommand_stores_current_and_new_password()
    {
        var command = new ChangePasswordCommand("OldPass123!", "NewPass456!");

        Assert.Equal("OldPass123!", command.CurrentPassword);
        Assert.Equal("NewPass456!", command.NewPassword);
    }
}
