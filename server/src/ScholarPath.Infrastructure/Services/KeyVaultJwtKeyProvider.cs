using System.Security.Cryptography;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Production <see cref="IJwtKeyProvider"/>. Fetches the RSA signing key from
/// Azure Key Vault and keeps it in memory for the lifetime of the process.
/// <para>
/// The RSA private key is stored in the vault as a <b>secret</b> in PEM format
/// (secret name <c>Jwt:KeyName</c>). Storing it as a secret — rather than as a
/// Key Vault <i>key</i> — lets the API export the private material once and
/// sign tokens locally with a real <see cref="RsaSecurityKey"/>, instead of a
/// network round-trip to the vault for every signature. Authentication uses
/// <see cref="DefaultAzureCredential"/>, so the same code works with a managed
/// identity in Azure and with developer credentials locally.
/// </para>
/// <para>
/// The vault is only contacted once provisioned; until then this class simply
/// has to compile and be correctly wired. The fetch is lazy (<see cref="Lazy{T}"/>)
/// so a missing or empty vault fails loudly on first token use rather than
/// silently at app startup.
/// </para>
/// </summary>
public sealed class KeyVaultJwtKeyProvider : IJwtKeyProvider, IDisposable
{
    private readonly JwtOptions _opts;
    private readonly Lazy<RsaSecurityKey> _key;

    // Holds the RSA instance for disposal. The Lazy factory is the sole writer
    // and runs exactly once, so no extra synchronisation is needed for reads
    // after _key.Value has been observed.
    private RSA? _rsa;

    public KeyVaultJwtKeyProvider(IOptions<JwtOptions> jwtOptions)
    {
        ArgumentNullException.ThrowIfNull(jwtOptions);
        _opts = jwtOptions.Value;

        if (string.IsNullOrWhiteSpace(_opts.KeyVaultUri))
        {
            throw new InvalidOperationException(
                "KeyVaultJwtKeyProvider requires Jwt:KeyVaultUri to be configured.");
        }

        _key = new Lazy<RsaSecurityKey>(LoadFromVault, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string KeyId => _key.Value.KeyId;

    public RsaSecurityKey GetSigningKey() => _key.Value;

    public RsaSecurityKey GetValidationKey() => _key.Value;

    public string Describe(out bool isWarning)
    {
        isWarning = false;
        return $"JWT signing key sourced from Azure Key Vault '{_opts.KeyVaultUri}' "
             + $"(secret '{_opts.KeyName}', RS256). Fetched lazily on first token use.";
    }

    /// <summary>
    /// Downloads the RSA private-key PEM from Key Vault. Invoked once by the
    /// <see cref="Lazy{T}"/> on first key use; the result is cached thereafter.
    /// </summary>
    private RsaSecurityKey LoadFromVault()
    {
        var client = new SecretClient(new Uri(_opts.KeyVaultUri!), new DefaultAzureCredential());
        KeyVaultSecret secret = client.GetSecret(_opts.KeyName);

        // Assigned to the disposable-tracking field immediately so it is owned
        // by this instance and released in Dispose().
        _rsa = RSA.Create();
        _rsa.ImportFromPem(secret.Value);

        // Prefer the Key Vault secret version as the kid (it rotates with the
        // key); fall back to a public-key thumbprint when the version is absent.
        string keyId;
        if (secret.Properties?.Version is { Length: > 0 } version)
        {
            keyId = version;
        }
        else
        {
            var thumbprint = SHA256.HashData(_rsa.ExportSubjectPublicKeyInfo());
            keyId = Convert.ToHexString(thumbprint)[..16];
        }

        return new RsaSecurityKey(_rsa) { KeyId = keyId };
    }

    public void Dispose() => _rsa?.Dispose();
}
