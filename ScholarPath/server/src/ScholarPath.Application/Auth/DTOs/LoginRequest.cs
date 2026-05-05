namespace ScholarPath.Application.Auth.DTOs;

public record LoginRequest(
    string Identifier,
    string Password,
    bool RememberMe = false);
