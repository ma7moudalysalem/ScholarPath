using MediatR;
using Microsoft.AspNetCore.Identity;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Domain.Entities;
using AutoMapper;

namespace ScholarPath.Application.Auth.Queries.GetMe;

public class GetMeQueryHandler : IRequestHandler<GetMeQuery, UserDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public GetMeQueryHandler(
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            throw new UnauthorizedAccessException("errors.auth.userNotFound");
        }

        return _mapper.Map<UserDto>(user);
    }
}
