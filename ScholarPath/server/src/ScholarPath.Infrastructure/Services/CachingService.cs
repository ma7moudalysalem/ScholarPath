using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Infrastructure.Services;

public class CachingService : ICachingService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public CachingService(IDistributedCache cache, ILogger<CachingService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _cache.GetStringAsync(key, cancellationToken);

            if (data is null)
                return default;

            return JsonSerializer.Deserialize<T>(data, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get cache key '{Key}'. Returning default value.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = JsonSerializer.Serialize(value, JsonOptions);

            var options = new DistributedCacheEntryOptions();

            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry.Value;
            }
            else
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            }

            await _cache.SetStringAsync(key, data, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cache key '{Key}'. Operation skipped.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cache key '{Key}'. Operation skipped.", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _cache.GetStringAsync(key, cancellationToken);
            return data is not null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check cache key '{Key}'. Returning false.", key);
            return false;
        }
    }
}
