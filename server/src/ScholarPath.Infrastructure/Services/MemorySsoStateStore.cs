using Microsoft.Extensions.Caching.Memory;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// <see cref="IMemoryCache"/>-backed <see cref="ISsoStateStore"/> (SEC-06 / GAP-2).
/// Nonces live for 10 minutes — long enough to complete an OAuth round-trip, short
/// enough to bound replay. Single-use: <see cref="Consume"/> removes on read.
///
/// NOTE: in-process only. A scaled-out deployment where the authorize redirect and
/// the callback can land on different instances must swap this for an
/// <c>IDistributedCache</c>-backed implementation (same interface).
/// </summary>
public sealed class MemorySsoStateStore(IMemoryCache cache) : ISsoStateStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    private static string Key(string state) => $"sso-state:{state}";

    public void Store(string state)
    {
        if (string.IsNullOrWhiteSpace(state)) return;
        cache.Set(Key(state), true, Ttl);
    }

    public bool Consume(string state)
    {
        if (string.IsNullOrWhiteSpace(state)) return false;
        var key = Key(state);
        if (cache.TryGetValue(key, out _))
        {
            cache.Remove(key);
            return true;
        }
        return false;
    }
}
