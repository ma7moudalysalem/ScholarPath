using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ScholarPath.Application.Auth.Commands.ConfirmEmailChange;
using ScholarPath.Application.Auth.Commands.ForgotPassword;
using ScholarPath.Application.Auth.Commands.Login;
using ScholarPath.Application.Auth.Commands.Logout;
using ScholarPath.Application.Auth.Commands.RefreshToken;
using ScholarPath.Application.Auth.Commands.Register;
using ScholarPath.Application.Auth.Commands.RequestEmailChange;
using ScholarPath.Application.Auth.Commands.ResendVerificationEmail;
using ScholarPath.Application.Auth.Commands.ResetPassword;
using ScholarPath.Application.Auth.Commands.SelectRole;
using ScholarPath.Application.Auth.Commands.SsoLogin;
using ScholarPath.Application.Auth.Commands.SwitchRole;
using ScholarPath.Application.Auth.Commands.VerifyEmail;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Auth.Queries.GetCurrentUser;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(IMediator mediator, ISsoService ssoService, ISsoStateStore ssoStateStore) : ControllerBase
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

    /// <summary>Email a one-time, 1-hour password-reset link. Always returns 204 (no account enumeration).</summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request, CancellationToken ct)
    {
        await mediator.Send(new ForgotPasswordCommand(request.Email), ct);
        return NoContent();
    }

    /// <summary>Consume a password-reset token, set the new password, and revoke all active sessions.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto request, CancellationToken ct)
    {
        await mediator.Send(new ResetPasswordCommand(request.Token, request.NewPassword), ct);
        return NoContent();
    }

    /// <summary>Confirm an email address using the token from the verification link (FR-215).</summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequestDto request, CancellationToken ct)
    {
        await mediator.Send(new VerifyEmailCommand(request.UserId, request.Token), ct);
        return NoContent();
    }

    /// <summary>Re-send the email-verification link. Always returns 204 (no account enumeration).</summary>
    [HttpPost("resend-verification")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationRequestDto request, CancellationToken ct)
    {
        await mediator.Send(new ResendVerificationEmailCommand(request.Email), ct);
        return NoContent();
    }

    /// <summary>Return the authenticated user's profile summary.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserDto>> GetMe(CancellationToken ct)
        => Ok(await mediator.Send(new GetCurrentUserQuery(), ct));

    /// <summary>Switch the active role for a dual-role account; re-issues the token pair.</summary>
    [HttpPost("switch-role")]
    [Authorize]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthTokensDto>> SwitchRole(
        [FromBody] SwitchRoleRequestDto request, CancellationToken ct)
        => Ok(await mediator.Send(new SwitchRoleCommand(request.TargetRole), ct));

    /// <summary>One-time first-role selection for a newly-registered account; re-issues the token pair.</summary>
    [HttpPost("select-role")]
    [Authorize]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AuthTokensDto>> SelectRole(
        [FromBody] SelectRoleRequestDto request, CancellationToken ct)
        => Ok(await mediator.Send(new SelectRoleCommand(request.Role, request.Details), ct));

    /// <summary>
    /// FR-231 — request a change to the registered email. Emails a confirmation
    /// link to the new address; the change is not applied until it is confirmed.
    /// </summary>
    [HttpPost("change-email")]
    [Authorize]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RequestEmailChange(
        [FromBody] RequestEmailChangeRequestDto request, CancellationToken ct)
    {
        await mediator.Send(new RequestEmailChangeCommand(request.NewEmail), ct);
        return NoContent();
    }

    /// <summary>
    /// FR-231 — confirm a pending email change using the token from the
    /// confirmation link. Must be called while signed in to the account.
    /// </summary>
    [HttpPost("change-email/confirm")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConfirmEmailChange(
        [FromBody] ConfirmEmailChangeRequestDto request, CancellationToken ct)
    {
        await mediator.Send(new ConfirmEmailChangeCommand(request.NewEmail, request.Token), ct);
        return NoContent();
    }

    [HttpGet("google/authorize")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult GoogleAuthorize([FromQuery] string redirectUri)
        => Redirect(ssoService.BuildGoogleAuthorizeUrl(redirectUri, IssueSsoState()));

    [HttpGet("google/callback")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthTokensDto>> GoogleCallback(
        [FromQuery] string code, [FromQuery] string redirectUri, [FromQuery] string? state, CancellationToken ct)
    {
        if (!ValidateSsoState(state)) return BadRequest("Invalid or expired SSO state.");
        return Ok(await mediator.Send(new SsoLoginCommand("Google", code, redirectUri), ct));
    }

    [HttpGet("microsoft/authorize")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult MicrosoftAuthorize([FromQuery] string redirectUri)
        => Redirect(ssoService.BuildMicrosoftAuthorizeUrl(redirectUri, IssueSsoState()));

    [HttpGet("microsoft/callback")]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthTokensDto>> MicrosoftCallback(
        [FromQuery] string code, [FromQuery] string redirectUri, [FromQuery] string? state, CancellationToken ct)
    {
        if (!ValidateSsoState(state)) return BadRequest("Invalid or expired SSO state.");
        return Ok(await mediator.Send(new SsoLoginCommand("Microsoft", code, redirectUri), ct));
    }

    // SEC-06 / GAP-2 — issue a single-use CSPRNG `state` nonce and remember it
    // server-side so the matching callback can prove the handshake it completes is
    // the one this server started (anti-CSRF / anti account-linking).
    private string IssueSsoState()
    {
        var state = System.Security.Cryptography.RandomNumberGenerator.GetHexString(32);
        ssoStateStore.Store(state);
        return state;
    }

    private bool ValidateSsoState(string? state)
        => !string.IsNullOrWhiteSpace(state) && ssoStateStore.Consume(state);

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? ClientUserAgent()
    {
        var ua = Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(ua) ? null : ua;
    }
}
