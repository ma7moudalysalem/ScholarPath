using MediatR;
using ScholarPath.Application.Profile.DTOs;

namespace ScholarPath.Application.Profile.Queries.GetProfile;

public sealed record GetProfileQuery : IRequest<UserProfileDto>;
