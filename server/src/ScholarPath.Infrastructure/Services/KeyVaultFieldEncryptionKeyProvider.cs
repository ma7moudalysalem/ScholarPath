using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Production <see cref="IFieldEncryptionKeyProvider"/>. Fetches the AES-256
/// field-encryption key from Azure Key Vault and keeps it in memory for the
/// lifetime of the process.
/// <para>
/// The key is stored in the vault as a <b>secret</b> whose value is the
/// Base64-encoding of 32 raw key bytes (secret name <c>FieldEncryption:KeyName</c>).
/// Authentication uses <see cref="DefaultAzureCredential"/>, so the same code
/// works with a managed identity in Azure and with developer credentials locally.
/// </para>
/// <para>
/// The fetch is lazy (<see cref="Lazy{T}"/>) so a missing or malformed vault
/// secret fails loudly on first encryption use rather than silently at app
/// startup — mirroring <see cref="KeyVaultJwtKeyProvider"/>.
/// </para>
/// </summary>
public sealed class KeyVaultFieldEncryptionKeyProvider : IFieldEncryptionKeyProvider
{
    private const int KeySizeBytes = 32; // AES-256

    private readonly FieldEncryptionOptions _opts;
    private readonly Lazy<byte[]> _key;

    public KeyVaultFieldEncryptionKeyProvider(IOptions<FieldEncryptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _opts = options.Value;

        if (string.IsNullOrWhiteSpace(_opts.KeyVaultUri))
        {
            throw new InvalidOperationException(
                "KeyVaultFieldEncryptionKeyProvider requires FieldEncryption:KeyVaultUri to be configured.");
        }

        _key = new Lazy<byte[]>(LoadFromVault, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public byte[] GetKey() => _key.Value;

    public string Describe(out bool isWarning)
    {
        isWarning = false;
        return $"Field-encryption key sourced from Azure Key Vault '{_opts.KeyVaultUri}' "
             + $"(secret '{_opts.KeyName}', AES-256-GCM). Fetched lazily on first use.";
    }

    /// <summary>
    /// Downloads the Base64 AES key from Key Vault. Invoked once by the
    /// <see cref="Lazy{T}"/> on first key use; the result is cached thereafter.
    /// </summary>
    private byte[] LoadFromVault()
    {
        var client = new SecretClient(new Uri(_opts.KeyVaultUri!), new DefaultAzureCredential());
        KeyVaultSecret secret = client.GetSecret(_opts.KeyName);

        byte[] key;
        try
        {
            key = Convert.FromBase64String(secret.Value);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"Key Vault secret '{_opts.KeyName}' is not valid Base64 — it must hold a "
              + "Base64-encoded 256-bit AES key.", ex);
        }

        if (key.Length != KeySizeBytes)
        {
            throw new InvalidOperationException(
                $"Key Vault secret '{_opts.KeyName}' decoded to {key.Length} bytes; "
              + $"a 256-bit AES key must be exactly {KeySizeBytes} bytes.");
        }

        return key;
    }
}
