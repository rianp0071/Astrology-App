using Microsoft.Extensions.Caching.Memory;

public class CacheService
{
    private readonly IMemoryCache _memoryCache;

    public CacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    // Method to clear a specific cache key
    public void ClearCache(string key)
    {
        _memoryCache.Remove(key);
    }

    // Method to get cached data
    public T? GetFromCache<T>(string key)
    {
        // If the key exists in cache and the value is not null, return it
        if (_memoryCache.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        // Otherwise, return default(T)
        return default;
    }

    // Method to set data in cache
    public void SetCache<T>(string key, T value, TimeSpan expiration)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        _memoryCache.Set(key, value, cacheOptions);
    }
}
