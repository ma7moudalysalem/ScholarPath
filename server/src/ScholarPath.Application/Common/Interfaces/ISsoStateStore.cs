namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Server-side store for the OAuth <c>state</c> anti-CSRF nonce (SEC-06 / GAP-2).
/// A value is issued when the SSO authorize redirect is built and consumed
/// (single-use) when the provider calls back, so an attacker cannot replay a
/// forged callback to hijack the OAuth handshake / link accounts.
/// </summary>
public interface ISsoStateStore
{
    /// <summary>Persist a freshly-minted state nonce with a short expiry.</summary>
    void Store(string state);

    /// <summary>
    /// Returns <c>true</c> and removes the nonce if it was present (valid, unexpired,
    /// unused); returns <c>false</c> for a missing / already-consumed / expired nonce.
    /// </summary>
    bool Consume(string state);
}
