using Microsoft.AspNetCore.DataProtection;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Stateless <see cref="ISsoStateStore"/> (SEC-06). The OAuth <c>state</c> is a
/// data-protected token that carries its own expiry, so it needs NO server-side
/// storage and therefore survives app restarts and multiple App Service instances.
///
/// This replaces the previous <c>IMemoryCache</c>-backed store whose nonce was lost
/// on every recycle/deploy — a user whose OAuth round-trip spanned a restart got
/// "Sign-in could not be completed." CSRF protection comes from the token being
/// unforgeable (ASP.NET DataProtection signs + encrypts it with a key ring that
/// App Service persists across restarts/instances); anti-replay is covered by the
/// OAuth authorization code being single-use at the provider.
/// </summary>
public sealed class DataProtectionSsoStateStore : ISsoStateStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
    private readonly IDataProtector _protector;

    public DataProtectionSsoStateStore(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("ScholarPath.Sso.State.v1");
    }

    public string Issue()
    {
        // payload = <expiry-unix-seconds>:<random-nonce>, protected (tamper-proof).
        var expiresAt = DateTimeOffset.UtcNow.Add(Ttl).ToUnixTimeSeconds();
        var payload = $"{expiresAt}:{Guid.NewGuid():N}";
        return _protector.Protect(payload);
    }

    public bool Validate(string? state)
    {
        if (string.IsNullOrWhiteSpace(state)) return false;

        try
        {
            var payload = _protector.Unprotect(state); // throws if forged/tampered
            var sep = payload.IndexOf(':');
            return sep > 0
                && long.TryParse(payload.AsSpan(0, sep), out var expiresAt)
                && DateTimeOffset.FromUnixTimeSeconds(expiresAt) > DateTimeOffset.UtcNow;
        }
        catch
        {
            return false;
        }
    }
}
