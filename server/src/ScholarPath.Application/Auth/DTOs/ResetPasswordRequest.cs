namespace ScholarPath.Application.Auth.DTOs;

public record ResetPasswordRequest(
    string Token,
    string NewPassword,
    string ConfirmNewPassword);
