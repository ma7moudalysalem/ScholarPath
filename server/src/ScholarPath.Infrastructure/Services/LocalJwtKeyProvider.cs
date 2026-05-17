using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Development / fallback <see cref="IJwtKeyProvider"/>. Loads an RSA private
/// key from a PEM file at <c>Jwt:DevKeyPath</c>; when that path is unset or the
/// file is missing it generates an ephemeral RSA-2048 key at startup so the API
/// still runs locally without any key provisioning.
/// <para>
/// An ephemeral key lives only for the lifetime of the process — every restart
/// invalidates previously issued tokens. That is acceptable for development but
/// never for production, which must use <see cref="KeyVaultJwtKeyProvider"/>.
/// </para>
/// </summary>
public sealed class LocalJwtKeyProvider : IJwtKeyProvider, IDisposable
{
    private readonly RSA _rsa;
    private readonly RsaSecurityKey _key;
    private readonly string _description;
    private readonly bool _isWarning;

    public LocalJwtKeyProvider(IOptions<JwtOptions> jwtOptions)
    {
        ArgumentNullException.ThrowIfNull(jwtOptions);

        var devKeyPath = jwtOptions.Value.DevKeyPath;
        _rsa = RSA.Create();

        if (!string.IsNullOrWhiteSpace(devKeyPath) && File.Exists(devKeyPath))
        {
            _rsa.ImportFromPem(File.ReadAllText(devKeyPath));
            _description =
                $"JWT signing key loaded from local PEM file '{devKeyPath}' (RS256, development).";
            _isWarning = false;
        }
        else
        {
            _rsa.KeySize = 2048;
            _isWarning = true;
            _description = string.IsNullOrWhiteSpace(devKeyPath)
                ? "No JWT dev key path configured — generated an ephemeral RSA-2048 signing key. "
                  + "Tokens will not survive an app restart. DEVELOPMENT ONLY; "
                  + "configure Jwt:KeyVaultUri for production."
                : $"JWT dev key path '{devKeyPath}' not found — generated an ephemeral RSA-2048 "
                  + "signing key. Tokens will not survive an app restart. DEVELOPMENT ONLY.";
        }

        // KeyId is derived from the public key so it stays stable for a given
        // key (and changes when the key changes), mirroring a Key Vault version.
        var publicKeyBytes = _rsa.ExportSubjectPublicKeyInfo();
        var thumbprint = SHA256.HashData(publicKeyBytes);
        _key = new RsaSecurityKey(_rsa)
        {
            KeyId = Convert.ToHexString(thumbprint)[..16],
        };
    }

    public string KeyId => _key.KeyId;

    public RsaSecurityKey GetSigningKey() => _key;

    public RsaSecurityKey GetValidationKey() => _key;

    public string Describe(out bool isWarning)
    {
        isWarning = _isWarning;
        return _description;
    }

    public void Dispose() => _rsa.Dispose();
}
