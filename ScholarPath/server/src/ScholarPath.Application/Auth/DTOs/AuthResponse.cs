namespace ScholarPath.Application.Auth.DTOs;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);
