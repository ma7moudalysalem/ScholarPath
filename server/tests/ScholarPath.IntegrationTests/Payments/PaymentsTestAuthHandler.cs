using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.IntegrationTests.Payments;

public sealed class PaymentsTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ICurrentUserService currentUser)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "PaymentsTest";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = currentUser.UserId ?? Guid.NewGuid();
        var role = currentUser.ActiveRole ?? "Student";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role,           role),
            new Claim(ClaimTypes.Email,
                currentUser.Email ?? "test@scholarpath.local"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
