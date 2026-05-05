using MediatR;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Commands.Register;

public record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : IRequest<AuthResult>;
