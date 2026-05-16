using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ScholarPath.Application.Auth.Commands.Login;
using ScholarPath.Application.Auth.Commands.Logout;
using ScholarPath.Application.Auth.Commands.RefreshToken;
using ScholarPath.Application.Auth.Commands.Register;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Auth.Queries.GetCurrentUser;

namespace ScholarPath.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>Register a new account (Unassigned status) and return the initial token pair.</summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AuthTokensDto>> Register(
        [FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var result = await mediator.Send(new RegisterCommand(
            request.Email, request.Password, request.FirstName, request.LastName,
            RememberMe: false, IpAddress: ClientIp(), UserAgent: ClientUserAgent()), ct);
        return Ok(result);
    }

    /// <summary>Authenticate with email + password. Locks the account after 5 failed attempts.</summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthTokensDto>> Login(
        [FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var result = await mediator.Send(new LoginCommand(
            request.Email, request.Password, request.RememberMe,
            ClientIp(), ClientUserAgent()), ct);
        return Ok(result);
    }

    /// <summary>Rotate a refresh token and return a fresh token pair.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthTokensDto>> Refresh(
        [FromBody] RefreshTokenRequestDto request, CancellationToken ct)
    {
        var result = await mediator.Send(new RefreshTokenCommand(
            request.RefreshToken, ClientIp(), ClientUserAgent()), ct);
        return Ok(result);
    }

    /// <summary>Revoke the supplied refresh token.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request, CancellationToken ct)
    {
        await mediator.Send(new LogoutCommand(request.RefreshToken), ct);
        return NoContent();
    }

    /// <summary>Return the authenticated user's profile summary.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserDto>> GetMe(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCurrentUserQuery(), ct);
        return Ok(result);
    }

    // ─── Still scaffolded — PB-001 remaining work ────────────────────────────
    // forgot/reset/change password, switch-role, Google/Microsoft SSO.

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        => NotImplementedForTeam("ForgotPasswordCommand");

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ResetPassword([FromBody] ResetPasswordRequestDto request)
        => NotImplementedForTeam("ResetPasswordCommand");

    [HttpPost("switch-role")]
    [Authorize]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public IActionResult SwitchRole([FromBody] SwitchRoleRequestDto request)
        => NotImplementedForTeam("SwitchRoleCommand");

    [HttpGet("google/authorize")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult GoogleAuthorize([FromQuery] string redirectUri)
        => NotImplementedForTeam("ISsoService.BuildGoogleAuthorizeUrl");

    [HttpGet("google/callback")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public IActionResult GoogleCallback([FromQuery] string code, [FromQuery] string redirectUri)
        => NotImplementedForTeam("GoogleSsoCallbackCommand");

    [HttpGet("microsoft/authorize")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult MicrosoftAuthorize([FromQuery] string redirectUri)
        => NotImplementedForTeam("ISsoService.BuildMicrosoftAuthorizeUrl");

    [HttpGet("microsoft/callback")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    public IActionResult MicrosoftCallback([FromQuery] string code, [FromQuery] string redirectUri)
        => NotImplementedForTeam("MicrosoftSsoCallbackCommand");

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? ClientUserAgent()
    {
        var ua = Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(ua) ? null : ua;
    }

    private NotFoundObjectResult NotImplementedForTeam(string nextStep) =>
        NotFound(new
        {
            status = "scaffold",
            message = $"Handler not yet implemented: {nextStep} (PB-001 remaining work).",
        });
}
