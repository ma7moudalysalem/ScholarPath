using MediatR;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Queries.GetMe;

public record GetMeQuery(string UserId) : IRequest<UserDto>;
