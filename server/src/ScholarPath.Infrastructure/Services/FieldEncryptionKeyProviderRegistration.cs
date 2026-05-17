using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Builds the AES-256 <see cref="IFieldEncryptionKeyProvider"/> chosen by
/// configuration and guarantees a <b>single shared instance</b> per
/// <see cref="IConfiguration"/> — mirroring <see cref="JwtKeyProviderRegistration"/>.
/// <para>
/// Memoising the provider keeps the (lazy) Key Vault fetch to a single network
/// round-trip and ensures every consumer encrypts and decrypts with the very
/// same key bytes.
/// </para>
/// </summary>
public static class FieldEncryptionKeyProviderRegistration
{
    // Keyed on the configuration instance: a single app has one IConfiguration,
    // and ConditionalWeakTable lets it be collected with the host.
    private static readonly ConditionalWeakTable<IConfiguration, IFieldEncryptionKeyProvider> Cache = new();

    /// <summary>
    /// Returns the field-encryption key provider for this configuration, creating
    /// it on first call. Key Vault when <c>FieldEncryption:KeyVaultUri</c> is set
    /// (production), the local Base64-key provider otherwise (development).
    /// </summary>
    public static IFieldEncryptionKeyProvider GetOrCreate(IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return Cache.GetValue(config, Create);
    }

    private static IFieldEncryptionKeyProvider Create(IConfiguration config)
    {
        var fieldOptions = config.GetSection(FieldEncryptionOptions.SectionName)
            .Get<FieldEncryptionOptions>() ?? new FieldEncryptionOptions();
        var options = Options.Create(fieldOptions);

        return string.IsNullOrWhiteSpace(fieldOptions.KeyVaultUri)
            ? new LocalFieldEncryptionKeyProvider(options)
            : new KeyVaultFieldEncryptionKeyProvider(options);
    }
}
