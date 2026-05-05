namespace ScholarPath.Application.Auth.DTOs;

public record AuthResult(
    string AccessToken,
    string RefreshToken,
    AuthResponse Response);
