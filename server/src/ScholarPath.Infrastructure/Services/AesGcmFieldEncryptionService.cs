using System.Security.Cryptography;
using System.Text;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// AES-256-GCM implementation of <see cref="IFieldEncryptionService"/> — the
/// application-level encryption layer for sensitive database columns at rest
/// (SRS security NFR).
/// <para>
/// <b>Ciphertext envelope.</b> A stored value is the literal prefix
/// <c>enc:v1:</c> followed by the Base64 of <c>nonce(12) ‖ tag(16) ‖ ciphertext</c>.
/// GCM is an AEAD cipher: the 16-byte authentication tag lets <see cref="Decrypt"/>
/// detect any tampering (a flipped bit, a truncated value) and refuse it. A fresh
/// random 12-byte nonce per call means two encryptions of the same plaintext
/// produce different ciphertext.
/// </para>
/// <para>
/// <b>Versioning.</b> The <c>v1</c> segment identifies the key/scheme. A future
/// key rotation can introduce <c>enc:v2:</c> and still decrypt old <c>v1</c>
/// values.
/// </para>
/// <para>
/// <b>Legacy plaintext pass-through.</b> <see cref="Decrypt"/> returns any value
/// that does not start with <see cref="EnvelopePrefix"/> unchanged. Rows written
/// before this feature shipped are plaintext, so the rollout is safe — they keep
/// reading correctly and are re-encrypted on their next write.
/// </para>
/// </summary>
public sealed class AesGcmFieldEncryptionService : IFieldEncryptionService
{
    /// <summary>Literal marker that identifies a value as a v1 ciphertext envelope.</summary>
    public const string EnvelopePrefix = "enc:v1:";

    private const int NonceSizeBytes = 12; // AesGcm.NonceByteSizes standard size
    private const int TagSizeBytes = 16;   // AesGcm.TagByteSizes maximum / standard size

    private readonly byte[] _key;

    public AesGcmFieldEncryptionService(IFieldEncryptionKeyProvider keyProvider)
    {
        ArgumentNullException.ThrowIfNull(keyProvider);

        _key = keyProvider.GetKey();
        if (_key.Length != 32)
        {
            throw new InvalidOperationException(
                $"AES-256-GCM requires a 32-byte key; the key provider returned {_key.Length} bytes.");
        }
    }

    /// <inheritdoc />
    public string? Encrypt(string? plaintext)
    {
        if (plaintext is null)
        {
            return null;
        }

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        var nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);

        var tag = new byte[TagSizeBytes];
        var ciphertext = new byte[plaintextBytes.Length];

        using (var aes = new AesGcm(_key, TagSizeBytes))
        {
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        }

        // Envelope payload: nonce ‖ tag ‖ ciphertext.
        var payload = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSizeBytes);
        Buffer.BlockCopy(tag, 0, payload, NonceSizeBytes, TagSizeBytes);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSizeBytes + TagSizeBytes, ciphertext.Length);

        return EnvelopePrefix + Convert.ToBase64String(payload);
    }

    /// <inheritdoc />
    public string? Decrypt(string? stored)
    {
        if (stored is null)
        {
            return null;
        }

        // Legacy plaintext (or any non-envelope value) passes through untouched —
        // this is what makes the rollout safe over pre-existing rows.
        if (!stored.StartsWith(EnvelopePrefix, StringComparison.Ordinal))
        {
            return stored;
        }

        var base64 = stored[EnvelopePrefix.Length..];

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(base64);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException(
                "Encrypted field value carries the 'enc:v1:' prefix but its payload is not valid Base64.",
                ex);
        }

        if (payload.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException(
                "Encrypted field value is too short to contain a nonce and authentication tag.");
        }

        var nonce = new byte[NonceSizeBytes];
        var tag = new byte[TagSizeBytes];
        var ciphertext = new byte[payload.Length - NonceSizeBytes - TagSizeBytes];
        Buffer.BlockCopy(payload, 0, nonce, 0, NonceSizeBytes);
        Buffer.BlockCopy(payload, NonceSizeBytes, tag, 0, TagSizeBytes);
        Buffer.BlockCopy(payload, NonceSizeBytes + TagSizeBytes, ciphertext, 0, ciphertext.Length);

        var plaintextBytes = new byte[ciphertext.Length];
        using (var aes = new AesGcm(_key, TagSizeBytes))
        {
            // Throws CryptographicException if the tag does not verify — i.e. the
            // value was tampered with or encrypted under a different key.
            aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);
        }

        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
