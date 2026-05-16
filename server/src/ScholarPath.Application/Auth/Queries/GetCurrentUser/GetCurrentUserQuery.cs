using MediatR;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<CurrentUserDto>;
