using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ScholarPath.Infrastructure.Persistence;

/// <summary>
/// EF Core caches the built model and, by default, keys that cache only by the
/// context CLR type. <see cref="ApplicationDbContext"/>'s model is not constant:
/// <c>OnModelCreating</c> wires the field-encryption <c>ValueConverter</c> only
/// when an <see cref="Application.Common.Interfaces.IFieldEncryptionService"/>
/// was injected. Two contexts of the same type — one encryption-aware, one not
/// (e.g. EF design-time tooling, or an in-memory test context) — would otherwise
/// share a single cached model, so whichever was built first would silently win.
/// <para>
/// This factory folds that distinction into the cache key, so the encryption-on
/// and encryption-off models are cached and reused independently.
/// </para>
/// </summary>
public sealed class EncryptionAwareModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        var encryptionEnabled = context is ApplicationDbContext { FieldEncryptionEnabled: true };
        return (context.GetType(), designTime, encryptionEnabled);
    }
}
