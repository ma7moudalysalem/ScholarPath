using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Auth.Commands.LinkProvider;
using ScholarPath.Application.Auth.Commands.ExternalLogin;
using ScholarPath.Application.Common.Models;
using Microsoft.Extensions.Options;

namespace ScholarPath.API.Controllers;

[Route("api/v{version:apiVersion}/auth/external")]
public class ExternalAuthController : BaseController
{
    private readonly IMediator _mediator;
    private readonly JwtSettings _jwtSettings;

    public ExternalAuthController(IMediator mediator, IOptions<JwtSettings> jwtSettings)
    {
        _mediator = mediator;
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>
    /// External login — validates provider token, finds or creates user, returns JWT.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> ExternalLogin([FromBody] ExternalLoginCommand command)
    {
        var result = await _mediator.Send(command);
        SetTokenCookies(result.AccessToken, result.RefreshToken);
        return Ok(result.Response);
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
            return Unauthorized(new { detail = "errors.auth.invalidAuthenticatedUser" });

        await _mediator.Send(new LinkProviderCommand(request.Provider, request.IdToken, userId));

        return Ok(new { Message = "Provider linked successfully." });
    }

    private void SetTokenCookies(string accessToken, string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Must be true in production, and works with localhost in modern browsers
            SameSite = SameSiteMode.Strict,
            IsEssential = true
        };

        // Access Token expires much sooner
        var accessOptions = cookieOptions;
        accessOptions.Expires = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        Response.Cookies.Append("AccessToken", accessToken, accessOptions);

        // Refresh Token lives longer
        var refreshOptions = cookieOptions;
        refreshOptions.Expires = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        Response.Cookies.Append("RefreshToken", refreshToken, refreshOptions);
    }
}
