using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Application.Common.Models;
using ScholarPath.Application.Auth.Commands.Login;
using ScholarPath.Application.Auth.Commands.Register;
using ScholarPath.Application.Auth.Commands.CompleteOnboarding;
using ScholarPath.Application.Auth.Commands.RefreshToken;
using ScholarPath.Application.Auth.Queries.GetMe;
using ScholarPath.Application.Auth.Commands.Logout;
namespace ScholarPath.API.Controllers;

[Route("api/v{version:apiVersion}/auth")]
public class AuthController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly JwtSettings _jwtSettings;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<CompleteOnboardingRequest> _completeOnboardingValidator;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        ApplicationDbContext dbContext,
        IMapper mapper,
        IOptions<JwtSettings> jwtSettings,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<CompleteOnboardingRequest> completeOnboardingValidator,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
        _mapper = mapper;
        _jwtSettings = jwtSettings.Value;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _completeOnboardingValidator = completeOnboardingValidator;
        _env = env;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequestResult(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        try
        {
            var command = new RegisterCommand(request.FirstName, request.LastName, request.Email, request.Password);
            var result = await Mediator.Send(command, cancellationToken);
            SetTokenCookies(result.AccessToken, result.RefreshToken);
            return CreatedAtAction(nameof(Me), result.Response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(new[] { ex.Message });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequestResult(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        try
        {
            var command = new LoginCommand(request.Identifier, request.Password, request.RememberMe);
            var result = await Mediator.Send(command, cancellationToken);
            SetTokenCookies(result.AccessToken, result.RefreshToken);
            return OkResult(result.Response);
        }
        catch (UnauthorizedAccessException ex)
        {
            if (ex.Message == "errors.auth.accountLockedOut")
            {
                return UnauthorizedResult("errors.auth.accountLockedOut");
            }
            if (ex.Message == "errors.auth.accountNotActive")
            {
                return ForbiddenResult("errors.auth.accountNotActive");
            }

            return UnauthorizedResult("errors.auth.invalidCredentials");
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> RefreshToken(CancellationToken cancellationToken)
    {
        // Read the refresh token from the HttpOnly cookie
        var refreshTokenValue = Request.Cookies["RefreshToken"];
        if (string.IsNullOrWhiteSpace(refreshTokenValue))
        {
            return UnauthorizedResult("errors.auth.invalidRefreshToken");
        }

        try
        {
            var command = new RefreshTokenCommand(refreshTokenValue);
            var result = await Mediator.Send(command, cancellationToken);
            SetTokenCookies(result.AccessToken, result.RefreshToken);
            return OkResult(result.Response);
        }
        catch (UnauthorizedAccessException ex)
        {
            if (ex.Message == "errors.auth.accountNotActive")
            {
                return ForbiddenResult(ex.Message);
            }
            return UnauthorizedResult(ex.Message);
        }
    }
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshTokenValue = Request.Cookies["RefreshToken"] ?? string.Empty;

        var command = new LogoutCommand(refreshTokenValue);
        await Mediator.Send(command, cancellationToken);

        Response.Cookies.Delete("AccessToken");
        Response.Cookies.Delete("RefreshToken");

        return NoContent();
    }

    [HttpPost("onboarding")]
    [Authorize]
    public async Task<ActionResult<UserDto>> CompleteOnboarding([FromBody] CompleteOnboardingRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _completeOnboardingValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequestResult(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        try
        {
            var command = new CompleteOnboardingCommand(
                request.SelectedRole,
                request.CompanyName,
                request.ExpertiseArea,
                request.Bio);

            var result = await Mediator.Send(command, cancellationToken);
            return OkResult(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            if (ex.Message == "errors.auth.userNotFound") return UnauthorizedResult(ex.Message);
            if (ex.Message == "errors.auth.accountNotEligibleForOnboarding") return ForbiddenResult(ex.Message);
            return UnauthorizedResult(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "errors.auth.onboardingAlreadyComplete" ||
                ex.Message == "errors.auth.pendingUpgradeExists")
            {
                return Conflict(new { Error = ex.Message });
            }
            return BadRequestResult(new[] { ex.Message });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return UnauthorizedResult("errors.auth.userNotFound");
        }

        try
        {
            var query = new GetMeQuery(userId);
            var result = await Mediator.Send(query, cancellationToken);
            return OkResult(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedResult(ex.Message);
        }
    }



    private string GetRequestIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private void SetTokenCookies(string accessToken, string refreshToken)
    {
        var isProduction = !_env.IsDevelopment();

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,       // false on http://localhost, true in production
            SameSite = SameSiteMode.Lax, // Lax allows top-level navigations; Strict was blocking refresh
            IsEssential = true
        };

        Response.Cookies.Append("AccessToken", accessToken, new CookieOptions
        {
            HttpOnly = cookieOptions.HttpOnly,
            Secure = cookieOptions.Secure,
            SameSite = cookieOptions.SameSite,
            IsEssential = cookieOptions.IsEssential,
            Expires = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
        });

        Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = cookieOptions.HttpOnly,
            Secure = cookieOptions.Secure,
            SameSite = cookieOptions.SameSite,
            IsEssential = cookieOptions.IsEssential,
            Expires = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        });
    }
}
