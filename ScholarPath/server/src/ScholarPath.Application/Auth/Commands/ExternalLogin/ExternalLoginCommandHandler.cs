using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Application.Common.Models;
using System.Net.Http.Json;

namespace ScholarPath.Application.Auth.Commands.ExternalLogin;

public class ExternalLoginCommandHandler : IRequestHandler<ExternalLoginCommand, AuthResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<ExternalLoginCommandHandler> _logger;

    public ExternalLoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IApplicationDbContext context,
        IMapper mapper,
        IOptions<JwtSettings> jwtSettings,
        ILogger<ExternalLoginCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
        _mapper = mapper;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<AuthResult> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.IdToken))
            throw new ValidationException(new[] { new ValidationFailure("", "errors.validation.required") });

        string? email;
        string? firstName;
        string? lastName;

        try
        {
            (email, firstName, lastName) = request.Provider.ToLowerInvariant() switch
            {
                "google" => await ValidateGoogleTokenAsync(request.IdToken, cancellationToken),
                "microsoft" => await ValidateMicrosoftTokenAsync(request.IdToken, cancellationToken),
                _ => throw new ValidationException(new[] { new ValidationFailure("", "errors.auth.unsupportedProvider") })
            };
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating external token");
            throw new ValidationException(new[] { new ValidationFailure("", "errors.auth.unsupportedProvider") });
        }

        if (string.IsNullOrEmpty(email))
            throw new UnauthorizedAccessException("errors.auth.invalidExternalToken");

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            var logins = await _userManager.GetLoginsAsync(existingUser);
            var isLinked = logins.Any(l => l.LoginProvider.Equals(request.Provider, StringComparison.OrdinalIgnoreCase));

            if (!isLinked)
                throw new ValidationException(new[] { new ValidationFailure("", "errors.auth.accountNotLinked") });

            if (existingUser.AccountStatus == AccountStatus.Suspended)
                throw new UnauthorizedAccessException("errors.auth.accountNotActive");

            existingUser.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(existingUser);

            return await GenerateAuthResponseAsync(existingUser, cancellationToken);
        }

        var newUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName ?? email.Split('@')[0],
            LastName = lastName ?? "",
            Role = UserRole.Unassigned,
            AccountStatus = AccountStatus.Active,
            IsOnboardingComplete = false,
            IsActive = true,
            LastLoginAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
            throw new ValidationException(createResult.Errors.Select(e => new ValidationFailure("", e.Description)));

        var loginInfo = new UserLoginInfo(request.Provider, request.ProviderKey ?? email, request.Provider);
        await _userManager.AddLoginAsync(newUser, loginInfo);

        return await GenerateAuthResponseAsync(newUser, cancellationToken);
    }

    private async Task<AuthResult> GenerateAuthResponseAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var accessToken = await _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var ipAddress = "unknown";

        var refreshToken = new ScholarPath.Domain.Entities.RefreshToken
        {
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            UserId = user.Id,
            CreatedByIp = ipAddress
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new AuthResponse(
            DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            _mapper.Map<UserDto>(user));

        return new AuthResult(accessToken, refreshTokenValue, response);
    }

    private static async Task<(string? Email, string? FirstName, string? LastName)> ValidateGoogleTokenAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={token}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo", cancellationToken);
                if (!response.IsSuccessStatusCode) return (null, null, null);
            }

            var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: cancellationToken);
            if (payload == null) return (null, null, null);

            var email = payload.GetValueOrDefault("email")?.ToString();
            var firstName = payload.GetValueOrDefault("given_name")?.ToString();
            var lastName = payload.GetValueOrDefault("family_name")?.ToString();

            return (email, firstName, lastName);
        }
        catch
        {
            return (null, null, null);
        }
    }

    private static async Task<(string? Email, string? FirstName, string? LastName)> ValidateMicrosoftTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me", cancellationToken);
            if (!response.IsSuccessStatusCode) return (null, null, null);

            var profile = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: cancellationToken);
            if (profile == null) return (null, null, null);

            var email = profile.GetValueOrDefault("mail")?.ToString()
                     ?? profile.GetValueOrDefault("userPrincipalName")?.ToString();
            var firstName = profile.GetValueOrDefault("givenName")?.ToString();
            var lastName = profile.GetValueOrDefault("surname")?.ToString();

            return (email, firstName, lastName);
        }
        catch
        {
            return (null, null, null);
        }
    }
}
