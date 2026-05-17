using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Builds the RS256 <see cref="IJwtKeyProvider"/> chosen by configuration and
/// guarantees a <b>single shared instance</b> per <see cref="IConfiguration"/>.
/// <para>
/// The token signer (private key) and the JWT-bearer validator (public key)
/// must use the same RSA key. With the development provider that key is
/// ephemeral, so two separately constructed providers would each mint a
/// different key and every token would fail validation. Composition happens in
/// two places — <c>AddInfrastructureServices</c> registers the provider in DI,
/// and application startup needs the public key for <c>AddJwtBearer</c> — so
/// the instance is memoised here and both call sites receive the same object.
/// </para>
/// </summary>
public static class JwtKeyProviderRegistration
{
    // Keyed on the configuration instance: a single app has one IConfiguration,
    // and ConditionalWeakTable lets it be collected with the host.
    private static readonly ConditionalWeakTable<IConfiguration, IJwtKeyProvider> Cache = new();

    /// <summary>
    /// Returns the JWT key provider for this configuration, creating it on first
    /// call. Key Vault when <c>Jwt:KeyVaultUri</c> is set (production), the local
    /// PEM / ephemeral-key provider otherwise (development).
    /// </summary>
    public static IJwtKeyProvider GetOrCreate(IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return Cache.GetValue(config, Create);
    }

    private static IJwtKeyProvider Create(IConfiguration config)
    {
        var jwtOptions = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var options = Options.Create(jwtOptions);

        return string.IsNullOrWhiteSpace(jwtOptions.KeyVaultUri)
            ? new LocalJwtKeyProvider(options)
            : new KeyVaultJwtKeyProvider(options);
    }
}
