namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Application-level encryption of individual sensitive database columns at rest
/// (SRS security NFR). Azure SQL Transparent Data Encryption already encrypts the
/// whole database file; this is a second, stronger layer — the affected columns
/// stay ciphertext even to someone with direct <c>SELECT</c> access to the
/// database, because the key never lives in SQL Server.
/// <para>
/// Encryption is applied transparently by an EF Core <c>ValueConverter</c>, so
/// command/query handlers never call this directly — they keep working with
/// plaintext values.
/// </para>
/// </summary>
public interface IFieldEncryptionService
{
    /// <summary>
    /// Encrypts <paramref name="plaintext"/> and returns the storable ciphertext
    /// envelope (a versioned, self-describing string). A <see langword="null"/>
    /// input returns <see langword="null"/>.
    /// </summary>
    string? Encrypt(string? plaintext);

    /// <summary>
    /// Reverses <see cref="Encrypt"/>. A value that is not a recognised ciphertext
    /// envelope is returned unchanged — pre-existing plaintext rows therefore keep
    /// working and the rollout is safe. A tampered envelope throws.
    /// </summary>
    string? Decrypt(string? stored);
}
