using Microsoft.Extensions.Options;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Development / fallback <see cref="IFieldEncryptionKeyProvider"/>. Reads a
/// Base64-encoded 256-bit AES key from <c>FieldEncryption:DevKey</c>.
/// <para>
/// Unlike <see cref="LocalJwtKeyProvider"/> — which can safely generate an
/// ephemeral RSA key — a field-encryption key <b>must</b> be stable across
/// restarts: an ephemeral key would make every previously-encrypted column value
/// permanently unreadable. So this provider has no generate-on-startup fallback;
/// a missing or malformed dev key is a hard configuration error.
/// </para>
/// <para>
/// The configured dev key is a fixed, well-known value committed to the
/// development / testing appsettings. That is acceptable for development but
/// never for production, which must use <see cref="KeyVaultFieldEncryptionKeyProvider"/>.
/// </para>
/// </summary>
public sealed class LocalFieldEncryptionKeyProvider : IFieldEncryptionKeyProvider
{
    private const int KeySizeBytes = 32; // AES-256

    private readonly byte[] _key;

    public LocalFieldEncryptionKeyProvider(IOptions<FieldEncryptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var devKey = options.Value.DevKey;
        if (string.IsNullOrWhiteSpace(devKey))
        {
            throw new InvalidOperationException(
                "FieldEncryption:DevKey is required when FieldEncryption:KeyVaultUri is not set. "
              + "It must hold a fixed Base64-encoded 256-bit AES key — field encryption needs a "
              + "key that is stable across restarts, so an ephemeral key is not acceptable.");
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(devKey);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "FieldEncryption:DevKey is not valid Base64 — it must hold a Base64-encoded "
              + "256-bit AES key.", ex);
        }

        if (key.Length != KeySizeBytes)
        {
            throw new InvalidOperationException(
                $"FieldEncryption:DevKey decoded to {key.Length} bytes; a 256-bit AES key must "
              + $"be exactly {KeySizeBytes} bytes.");
        }

        _key = key;
    }

    public byte[] GetKey() => _key;

    public string Describe(out bool isWarning)
    {
        isWarning = true;
        return "Field-encryption key loaded from the configured FieldEncryption:DevKey "
             + "(AES-256-GCM). DEVELOPMENT ONLY — configure FieldEncryption:KeyVaultUri for production.";
    }
}
