namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Issues and validates the OAuth <c>state</c> anti-CSRF token (SEC-06 / GAP-2).
/// The token is minted when the SSO authorize redirect is built and validated when
/// the provider calls back, so an attacker cannot forge a callback to hijack the
/// OAuth handshake / link accounts. Implementations MUST be stateless (or use a
/// persistent/shared store) so the token survives app restarts and multiple
/// instances — an in-process store loses it on every recycle/deploy, which broke
/// SSO sign-in ("Sign-in could not be completed").
/// </summary>
public interface ISsoStateStore
{
    /// <summary>Mints a fresh, tamper-proof, short-lived state token.</summary>
    string Issue();

    /// <summary>
    /// Returns <c>true</c> when the token is authentic (untampered) and unexpired;
    /// <c>false</c> for a missing / forged / expired token.
    /// </summary>
    bool Validate(string? state);
}
