using MediatR;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress,
    string? UserAgent) : IRequest<AuthTokensDto>;
