using MediatR;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Domain.Enums;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;
    private readonly IMapper _mapper;

    public RefreshTokenCommandHandler(
        ITokenService tokenService,
        IApplicationDbContext dbContext,
        IOptions<JwtSettings> jwtSettings,
        IMapper mapper)
    {
        _tokenService = tokenService;
        _dbContext = dbContext;
        _jwtSettings = jwtSettings.Value;
        _mapper = mapper;
    }

    public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.CurrentRefreshToken, cancellationToken);

        if (refreshToken is null || refreshToken.IsRevoked || refreshToken.IsExpired)
        {
            throw new UnauthorizedAccessException("errors.auth.invalidRefreshToken");
        }

        if (!refreshToken.User.IsActive || refreshToken.User.AccountStatus is AccountStatus.Suspended or AccountStatus.Rejected)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = "unknown"; // Ideally injected from ICurrentUserService
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("errors.auth.accountNotActive");
        }

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = "unknown";

        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        refreshToken.ReplacedByToken = newRefreshTokenValue;

        var replacementToken = new ScholarPath.Domain.Entities.RefreshToken
        {
            UserId = refreshToken.UserId,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedByIp = "unknown"
        };
        
        _dbContext.RefreshTokens.Add(replacementToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = await _tokenService.GenerateAccessToken(refreshToken.User);
        
        var response = new AuthResponse(
            DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            _mapper.Map<UserDto>(refreshToken.User));

        return new AuthResult(accessToken, replacementToken.Token, response);
    }
}
