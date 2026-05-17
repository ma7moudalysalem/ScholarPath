using Microsoft.IdentityModel.Tokens;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Supplies the RSA key material used to sign and validate JWT access tokens
/// (RS256). The token signer needs the private key; the JWT bearer middleware
/// only needs the public key. Implementations decide where the key comes from:
/// Azure Key Vault in production, a local PEM file (or an ephemeral key) in
/// development.
/// </summary>
public interface IJwtKeyProvider
{
    /// <summary>
    /// Stable identifier for the current key, surfaced as the JWT <c>kid</c>
    /// header so validators can pick the matching key during rotation.
    /// </summary>
    string KeyId { get; }

    /// <summary>
    /// Signing key — carries the RSA <b>private</b> key. Used by the token
    /// service to produce RS256 signatures.
    /// </summary>
    RsaSecurityKey GetSigningKey();

    /// <summary>
    /// Validation key — carries (at least) the RSA <b>public</b> key. Used by
    /// <c>AddJwtBearer</c> as the <c>IssuerSigningKey</c>.
    /// </summary>
    RsaSecurityKey GetValidationKey();

    /// <summary>
    /// One-line description of where the key came from and any caveats (e.g. an
    /// ephemeral dev key). The providers are constructed before the host's
    /// logging pipeline exists, so startup logs this through the real logger
    /// once the app is built. <see langword="true"/> in <paramref name="isWarning"/>
    /// marks a development-only situation that must not occur in production.
    /// </summary>
    string Describe(out bool isWarning);
}
