using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Production <see cref="IPasswordHasher"/> implementation. Delegates to ASP.NET
/// Core Identity's <c>PasswordHasher&lt;T&gt;</c> (PBKDF2 with a per-hash salt),
/// kept behind the <see cref="IPasswordHasher"/> interface so callers stay
/// decoupled from Identity and the hasher can be substituted in tests.
/// </summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(new object(), password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(new object(), hash, password)
            is Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success
            or Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded;
}
