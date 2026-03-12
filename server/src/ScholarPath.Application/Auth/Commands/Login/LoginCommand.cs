using MediatR;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Commands.Login;

public record LoginCommand(
    string Identifier,
    string Password,
    bool RememberMe) : IRequest<AuthResult>;
