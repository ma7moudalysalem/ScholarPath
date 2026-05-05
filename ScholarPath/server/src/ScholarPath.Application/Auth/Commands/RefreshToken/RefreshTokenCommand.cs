using MediatR;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(
    string CurrentRefreshToken) : IRequest<AuthResult>;
