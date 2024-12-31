using Microsoft.Extensions.Caching.Memory;

namespace services;

class CacheService
{
    private readonly IMemoryCache _cache;

    // `cache` is resolved from the DI container at runtime
    public CacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void CacheProcessedVideo(string videoId, object videoData)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8)
        };

        _cache.Set(videoId, videoData, cacheEntryOptions);
    }

    public object? GetProcessedVideo(string videoId)
    {
        _cache.TryGetValue(videoId, out var videoData);
        return videoData;
    }
}
