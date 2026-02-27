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
using ScholarPath.Infrastructure.Settings;

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
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;
    private readonly IValidator<CompleteOnboardingRequest> _completeOnboardingValidator;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        ApplicationDbContext dbContext,
        IMapper mapper,
        IOptions<JwtSettings> jwtSettings,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshTokenValidator,
        IValidator<CompleteOnboardingRequest> completeOnboardingValidator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
        _mapper = mapper;
        _jwtSettings = jwtSettings.Value;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _completeOnboardingValidator = completeOnboardingValidator;
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

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Conflict(new { Error = "errors.auth.emailAlreadyExists" });
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
            return BadRequestResult(createResult.Errors.Select(error => error.Description));
        }

        var authResponse = await CreateAuthResponseAsync(user, cancellationToken);
        return CreatedAtAction(nameof(Me), authResponse);
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

        var normalizedIdentifier = request.Identifier.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(normalizedIdentifier)
            ?? await _userManager.FindByNameAsync(normalizedIdentifier);

        if (user is null)
        {
            return UnauthorizedResult("errors.auth.invalidCredentials");
        }

        if (user.AccountStatus is AccountStatus.Suspended or AccountStatus.Rejected || !user.IsActive)
        {
            return ForbiddenResult("errors.auth.accountNotActive");
        }

        /*  var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
          if (!signInResult.Succeeded)
          {
              return UnauthorizedResult("errors.auth.invalidCredentials");
          }*/

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            return BadRequestResult("errors.auth.accountLockedOut");
        }

        if (!signInResult.Succeeded)
        {
            return UnauthorizedResult("errors.auth.invalidCredentials");
        }

        user.LastLoginAt = DateTime.UtcNow;
        var loginUpdateResult = await _userManager.UpdateAsync(user);
        if (!loginUpdateResult.Succeeded)
        {
            return BadRequestResult(loginUpdateResult.Errors.Select(error => error.Description));
        }

        var authResponse = await CreateAuthResponseAsync(user, cancellationToken);
        return OkResult(authResponse);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _refreshTokenValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequestResult(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        var refreshToken = await _dbContext.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.Token == request.RefreshToken, cancellationToken);

        if (refreshToken is null || refreshToken.IsRevoked || refreshToken.IsExpired)
        {
            return UnauthorizedResult("errors.auth.invalidRefreshToken");
        }

        if (!refreshToken.User.IsActive || refreshToken.User.AccountStatus is AccountStatus.Suspended or AccountStatus.Rejected)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = GetRequestIp();
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ForbiddenResult("errors.auth.accountNotActive");
        }

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = GetRequestIp();

        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        refreshToken.ReplacedByToken = newRefreshTokenValue;

        var replacementToken = CreateRefreshToken(refreshToken.UserId, newRefreshTokenValue);
        _dbContext.RefreshTokens.Add(replacementToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = await _tokenService.GenerateAccessToken(refreshToken.User);
        var response = new AuthResponse(
            accessToken,
            replacementToken.Token,
            DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            _mapper.Map<UserDto>(refreshToken.User));

        return OkResult(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return UnauthorizedResult("errors.auth.invalidAuthenticatedUser");
        }

        var tokensQuery = _dbContext.RefreshTokens
            .Where(token => token.UserId == parsedUserId && !token.IsRevoked && !token.IsExpired);

        if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            tokensQuery = tokensQuery.Where(token => token.Token == request.RefreshToken);
        }

        var tokensToRevoke = await tokensQuery.ToListAsync(cancellationToken);
        foreach (var token in tokensToRevoke)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = GetRequestIp();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
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

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return UnauthorizedResult("errors.auth.userNotFound");
        }

        if (user.IsOnboardingComplete)
        {
            return ConflictResult("errors.auth.onboardingAlreadyComplete");
        }

        if (user.AccountStatus is AccountStatus.Suspended or AccountStatus.Rejected)
        {
            return ForbiddenResult("errors.auth.accountNotEligibleForOnboarding");
        }

        if (request.SelectedRole == UserRole.Student)
        {
            user.Role = UserRole.Student;
            user.AccountStatus = AccountStatus.Active;
            user.IsOnboardingComplete = true;

            var studentUpdateResult = await _userManager.UpdateAsync(user);
            if (!studentUpdateResult.Succeeded)
            {
                return BadRequestResult(studentUpdateResult.Errors.Select(error => error.Description));
            }

            return OkResult(_mapper.Map<UserDto>(user));
        }

        var existingPendingRequest = await _dbContext.UpgradeRequests
            .AnyAsync(upgradeRequest =>
                upgradeRequest.UserId == user.Id &&
                upgradeRequest.Status == UpgradeRequestStatus.Pending, cancellationToken);

        if (existingPendingRequest)
        {
            return Conflict(new { Error = "errors.auth.pendingUpgradeExists" });
        }

        user.Role = UserRole.Unassigned;
        user.AccountStatus = AccountStatus.Pending;
        user.IsOnboardingComplete = true;

        var upgradeRequest = new UpgradeRequest
        {
            UserId = user.Id,
            RequestedRole = request.SelectedRole,
            Status = UpgradeRequestStatus.Pending,
            CompanyName = request.CompanyName,
            ExpertiseTags = request.ExpertiseArea,
            ExperienceSummary = request.Bio
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.UpgradeRequests.Add(upgradeRequest);

        var adminIds = await _dbContext.Users
            .Where(applicationUser => applicationUser.Role == UserRole.Admin && applicationUser.IsActive)
            .Select(applicationUser => applicationUser.Id)
            .ToListAsync(cancellationToken);

        foreach (var adminId in adminIds)
        {
            _dbContext.Notifications.Add(new Notification
            {
                UserId = adminId,
                Type = NotificationType.System,
                Title = "New upgrade request",
                Message = $"{user.FirstName} {user.LastName} requested {request.SelectedRole} access.",
                RelatedEntityId = upgradeRequest.Id,
                RelatedEntityType = nameof(UpgradeRequest)
            });
        }

        var upgradeUpdateResult = await _userManager.UpdateAsync(user);
        if (!upgradeUpdateResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return BadRequestResult(upgradeUpdateResult.Errors.Select(error => error.Description));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return OkResult(_mapper.Map<UserDto>(user));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return UnauthorizedResult("errors.auth.userNotFound");
        }

        return OkResult(_mapper.Map<UserDto>(user));
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var accessToken = await _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshToken = CreateRefreshToken(user.Id, refreshTokenValue);

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            refreshToken.Token,
            DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            _mapper.Map<UserDto>(user));
    }

    private RefreshToken CreateRefreshToken(Guid userId, string token)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedByIp = GetRequestIp()
        };
    }

    private string GetRequestIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
