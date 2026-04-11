using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    /// <summary>
    /// Register a new account. Creates user in `Unassigned` status and returns initial token pair.
    /// Teammates (@Madiha6776): wire `RegisterCommand` -> handler -> `AuthTokensDto`.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Register([FromBody] RegisterRequestDto request)
    {
        // @Madiha6776: wire: var result = await _mediator.Send(new RegisterCommand(...));
        return NotImplementedForTeam("RegisterCommand");
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public IActionResult Login([FromBody] LoginRequestDto request) => NotImplementedForTeam("LoginCommand");

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public IActionResult Refresh([FromBody] RefreshTokenRequestDto request) => NotImplementedForTeam("RefreshTokenCommand");

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout([FromBody] RefreshTokenRequestDto request) => NotImplementedForTeam("LogoutCommand");

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ForgotPassword([FromBody] ForgotPasswordRequestDto request) => NotImplementedForTeam("ForgotPasswordCommand");

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ResetPassword([FromBody] ResetPasswordRequestDto request) => NotImplementedForTeam("ResetPasswordCommand");

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public IActionResult GetMe() => NotImplementedForTeam("GetCurrentUserQuery");

    [HttpPost("switch-role")]
    [Authorize]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public IActionResult SwitchRole([FromBody] SwitchRoleRequestDto request) => NotImplementedForTeam("SwitchRoleCommand");

    [HttpGet("google/authorize")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult GoogleAuthorize([FromQuery] string redirectUri) => NotImplementedForTeam("ISsoService.BuildGoogleAuthorizeUrl");

    [HttpGet("google/callback")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public IActionResult GoogleCallback([FromQuery] string code, [FromQuery] string redirectUri) => NotImplementedForTeam("GoogleSsoCallbackCommand");

    [HttpGet("microsoft/authorize")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult MicrosoftAuthorize([FromQuery] string redirectUri) => NotImplementedForTeam("ISsoService.BuildMicrosoftAuthorizeUrl");

    [HttpGet("microsoft/callback")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public IActionResult MicrosoftCallback([FromQuery] string code, [FromQuery] string redirectUri) => NotImplementedForTeam("MicrosoftSsoCallbackCommand");

    private NotFoundObjectResult NotImplementedForTeam(string nextStep) =>
        NotFound(new
        {
            status = "scaffold",
            message = $"Handler not yet implemented. Next step for @Madiha6776: wire {nextStep} per .specify/specs/PB-001-auth-access-onboarding/tasks.md",
        });
}
