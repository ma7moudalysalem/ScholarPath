using MediatR;

namespace ScholarPath.Application.Auth.Commands.Logout;

public record LogoutCommand(
    string RefreshToken,
    bool LogoutEverywhere = false) : IRequest<Unit>;