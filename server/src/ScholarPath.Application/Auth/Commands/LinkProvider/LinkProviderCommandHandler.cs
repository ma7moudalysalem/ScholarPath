using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using ScholarPath.Domain.Entities;
using System.Net.Http.Json;

namespace ScholarPath.Application.Auth.Commands.LinkProvider;

public class LinkProviderCommandHandler : IRequestHandler<LinkProviderCommand, Unit>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public LinkProviderCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Unit> Handle(LinkProviderCommand request, CancellationToken cancellationToken)
    {
        // Actually, normally user Id is the user interacting. We passed it as string.
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            throw new UnauthorizedAccessException("errors.auth.userNotFound");

        string? email;

        try
        {
            (email, _, _) = request.Provider.ToLowerInvariant() switch
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
        catch (Exception)
        {
            throw new ValidationException(new[] { new ValidationFailure("", "errors.auth.invalidExternalToken") });
        }

        if (string.IsNullOrEmpty(email))
            throw new ValidationException(new[] { new ValidationFailure("", "errors.auth.invalidExternalToken") });

        var logins = await _userManager.GetLoginsAsync(user);
        if (logins.Any(l => l.LoginProvider.Equals(request.Provider, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ValidationException(new[] { new ValidationFailure("", "errors.auth.providerAlreadyLinked") });
        }

        var loginInfo = new UserLoginInfo(request.Provider, email, request.Provider);
        var result = await _userManager.AddLoginAsync(user, loginInfo);

        if (!result.Succeeded)
            throw new ValidationException(result.Errors.Select(e => new ValidationFailure("", e.Description)));

        return Unit.Value;
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
