namespace ScholarPath.Application.Auth.DTOs;

public record AuthResponse(
    DateTime ExpiresAt,
    UserDto User);
