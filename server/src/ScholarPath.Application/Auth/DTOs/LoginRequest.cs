namespace ScholarPath.Application.Auth.DTOs;

public record LoginRequest(
    string Email,
    string Password,
    bool? RememberMe = null 
);
