using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core <see cref="ValueConverter"/> that transparently encrypts a string
/// column on the way to the database and decrypts it on the way back, using
/// <see cref="IFieldEncryptionService"/> (AES-256-GCM).
/// <para>
/// Applying this converter to a property in <c>EntityConfigurations</c> is all
/// that is needed — command and query handlers keep reading and writing
/// plaintext and never learn that the column is encrypted at rest.
/// </para>
/// <para>
/// The converter is typed for <c>string?</c> so it can be attached directly to
/// the nullable PII properties. <see cref="IFieldEncryptionService.Encrypt"/> and
/// <see cref="IFieldEncryptionService.Decrypt"/> both map <see langword="null"/>
/// to <see langword="null"/>, so a null column value round-trips untouched.
/// </para>
/// </summary>
public sealed class EncryptedStringConverter : ValueConverter<string?, string?>
{
    public EncryptedStringConverter(IFieldEncryptionService encryption)
        : base(
            plaintext => encryption.Encrypt(plaintext),
            stored => encryption.Decrypt(stored))
    {
    }
}
