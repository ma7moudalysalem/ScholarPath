using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.API.Controllers;

[Route("api/v{version:apiVersion}/auth/external")]
public class ExternalAuthController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly JwtSettings _jwtSettings;

    public ExternalAuthController(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ApplicationDbContext context,
        IMapper mapper,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
        _mapper = mapper;
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>
    /// External login — validates provider token, finds or creates user, returns JWT.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> ExternalLogin([FromBody] ExternalLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.IdToken))
            return BadRequestResult("errors.validation.required");

        string? email;
        string? firstName;
        string? lastName;

        try
        {
            (email, firstName, lastName) = request.Provider.ToLowerInvariant() switch
            {
                "google" => await ValidateGoogleTokenAsync(request.IdToken),
                "microsoft" => await ValidateMicrosoftTokenAsync(request.IdToken),
                _ => throw new ArgumentException("errors.auth.unsupportedProvider")
            };
        }
        catch (ArgumentException)
        {
            return BadRequestResult("errors.auth.unsupportedProvider");
        }

        if (string.IsNullOrEmpty(email))
            return BadRequestResult("errors.auth.invalidExternalToken");

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            // Check if this provider is already linked
            var logins = await _userManager.GetLoginsAsync(existingUser);
            var isLinked = logins.Any(l => l.LoginProvider.Equals(request.Provider, StringComparison.OrdinalIgnoreCase));

            if (!isLinked)
            {
                return BadRequestResult("errors.auth.accountNotLinked");
            }

            // Check account status
            if (existingUser.AccountStatus == AccountStatus.Suspended)
                return ForbiddenResult("errors.auth.accountNotActive");

            // Update last login
            existingUser.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(existingUser);

            var authResponse = await GenerateAuthResponseAsync(existingUser);
            return OkResult(authResponse);
        }

        // Create new user (Unassigned by default, no password)
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
        {
            return BadRequestResult(createResult.Errors.Select(e => e.Description));
        }

        // Link the external provider
        var loginInfo = new UserLoginInfo(request.Provider, request.ProviderKey ?? email, request.Provider);
        await _userManager.AddLoginAsync(newUser, loginInfo);

        var newAuthResponse = await GenerateAuthResponseAsync(newUser);
        return CreatedAtAction(nameof(ExternalLogin), newAuthResponse);
    }

    /// <summary>
    /// Link an external provider to an existing authenticated account.
    /// </summary>
    [HttpPost("link")]
    [Authorize]
    public async Task<IActionResult> LinkProvider([FromBody] LinkProviderRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return UnauthorizedResult("errors.auth.invalidAuthenticatedUser");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFoundResult("errors.auth.userNotFound");

        // Check if already linked
        var logins = await _userManager.GetLoginsAsync(user);
        if (logins.Any(l => l.LoginProvider.Equals(request.Provider, StringComparison.OrdinalIgnoreCase)))
        {
            return ConflictResult("errors.auth.providerAlreadyLinked");
        }

        var loginInfo = new UserLoginInfo(request.Provider, request.ProviderKey ?? user.Email!, request.Provider);
        var result = await _userManager.AddLoginAsync(user, loginInfo);

        if (!result.Succeeded)
            return BadRequestResult(result.Errors.Select(e => e.Description));

        return Ok(new { Message = "Provider linked successfully." });
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user)
    {
        var accessToken = await _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            UserId = user.Id,
            CreatedByIp = ipAddress
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new AuthResponse(
            accessToken,
            refreshTokenValue,
            DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            _mapper.Map<UserDto>(user));
    }

    private static async Task<(string? Email, string? FirstName, string? LastName)> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");
            if (!response.IsSuccessStatusCode) return (null, null, null);

            var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
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

    private static async Task<(string? Email, string? FirstName, string? LastName)> ValidateMicrosoftTokenAsync(string accessToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");
            if (!response.IsSuccessStatusCode) return (null, null, null);

            var profile = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
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

// DTOs for external auth
public record ExternalLoginRequest(string Provider, string IdToken, string? ProviderKey = null);
public record LinkProviderRequest(string Provider, string ProviderKey);
