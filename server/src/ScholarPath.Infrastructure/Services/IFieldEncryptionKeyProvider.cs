namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Supplies the symmetric AES-256 key used by <c>AesGcmFieldEncryptionService</c>
/// to encrypt sensitive database columns at rest. Mirrors <see cref="IJwtKeyProvider"/>:
/// implementations decide where the key comes from — Azure Key Vault in production,
/// a configured Base64 key in development.
/// <para>
/// Unlike the JWT signing key, this key must be <b>stable across process restarts</b>
/// — an ephemeral key would make every previously-encrypted row permanently
/// unreadable. The development provider therefore reads a fixed configured key
/// rather than generating one.
/// </para>
/// </summary>
public interface IFieldEncryptionKeyProvider
{
    /// <summary>
    /// The 256-bit (32-byte) AES key. Callers must not mutate the returned array.
    /// </summary>
    byte[] GetKey();

    /// <summary>
    /// One-line description of where the key came from and any caveats (e.g. a
    /// development key). The providers are constructed before the host's logging
    /// pipeline exists, so startup logs this through the real logger once the app
    /// is built. <see langword="true"/> in <paramref name="isWarning"/> marks a
    /// development-only situation that must not occur in production.
    /// </summary>
    string Describe(out bool isWarning);
}
