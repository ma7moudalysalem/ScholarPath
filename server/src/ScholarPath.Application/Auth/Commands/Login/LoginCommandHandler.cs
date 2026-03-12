using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.Extensions.Options;

namespace ScholarPath.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;
    private readonly IMapper _mapper;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IApplicationDbContext dbContext,
        IOptions<JwtSettings> jwtSettings,
        IMapper mapper,
        ILogger<LoginCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
        _jwtSettings = jwtSettings.Value;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedIdentifier = request.Identifier.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(normalizedIdentifier)
            ?? await _userManager.FindByNameAsync(normalizedIdentifier);

        if (user is null)
        {
            _logger.LogWarning("Login attempt failed: user not found for identifier {Identifier}", request.Identifier);
            throw new UnauthorizedAccessException("errors.auth.invalidCredentials");
        }

        if (user.AccountStatus is AccountStatus.Suspended or AccountStatus.Rejected || !user.IsActive)
        {
            _logger.LogWarning("Login attempt blocked: account not active for user {UserId}", user.Id);
            throw new UnauthorizedAccessException("errors.auth.accountNotActive");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login attempt blocked: account locked out for user {UserId}", user.Id);
            throw new UnauthorizedAccessException("errors.auth.accountLockedOut");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Login failed: account locked after too many attempts for user {UserId}", user.Id);
                throw new UnauthorizedAccessException("errors.auth.accountLockedOut");
            }
            _logger.LogWarning("Login failed: invalid password for user {UserId}", user.Id);
            throw new UnauthorizedAccessException("errors.auth.invalidCredentials");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Login successful for user {UserId} ({Email})", user.Id, user.Email);

        return await CreateAuthResponseAsync(user, cancellationToken, request.RememberMe);
    }

    private async Task<AuthResult> CreateAuthResponseAsync(ApplicationUser user, CancellationToken cancellationToken, bool rememberMe = false)
    {
        var accessToken = await _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var expirationDays = rememberMe ? 30 : _jwtSettings.RefreshTokenExpirationDays;

        var refreshToken = new ScholarPath.Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            // CreatedByIp should be injected via a service if needed, falling back to empty for MediatR context
            CreatedByIp = "unknown"
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new AuthResponse(
            DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            _mapper.Map<UserDto>(user));

        return new AuthResult(accessToken, refreshToken.Token, response);
    }
}
