using MediatR;
using Microsoft.AspNetCore.Identity;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using AutoMapper;
using Microsoft.Extensions.Options;

namespace ScholarPath.Application.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;
    private readonly IMapper _mapper;

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IApplicationDbContext dbContext,
        IOptions<JwtSettings> jwtSettings,
        IMapper mapper)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
        _jwtSettings = jwtSettings.Value;
        _mapper = mapper;
    }

    public async Task<AuthResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            // Fix for S7 Account enumeration vulnerability. We should not leak that an account already exists.
            // Ideally we'd return success and send an email saying "An account exists", but for now raising an error with
            // a generic message.
            throw new InvalidOperationException("errors.auth.registrationFailed");
        }

        var user = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            UserName = request.Email.Trim().ToLowerInvariant(),
            Role = UserRole.Unassigned,
            AccountStatus = AccountStatus.Active,
            IsOnboardingComplete = false,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            // For simplicity, just combining errors, but in a real app might use a custom exception with a list
            throw new InvalidOperationException(string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    private async Task<AuthResult> CreateAuthResponseAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var accessToken = await _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        
        var refreshToken = new ScholarPath.Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedByIp = "unknown" // IP injection needed for a full implementation
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new AuthResponse(
            DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            _mapper.Map<UserDto>(user));

        return new AuthResult(accessToken, refreshToken.Token, response);
    }
}
