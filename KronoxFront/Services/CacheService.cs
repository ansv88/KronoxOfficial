using KronoxFront.ViewModels;
using Microsoft.Extensions.Caching.Memory;

namespace KronoxFront.Services;

// Tjänst för att hantera caching av dokumentlistor i minnet med IMemoryCache.
public class CacheService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _defaultCacheTime = TimeSpan.FromMinutes(15);
    private const string DocumentsCacheKey = "DocumentsWithCategories";

    public CacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    // Hämtar dokumentlistan från cachen, eller null om den inte finns.
    public List<DocumentViewModel>? GetDocumentsFromCache()
    {
        return _cache.TryGetValue(DocumentsCacheKey, out List<DocumentViewModel>? documents) ? documents : null;
    }

    // Lagrar dokumentlistan i cachen med fördefinierad utgångstid.
    public void SetDocumentsToCache(List<DocumentViewModel> documents)
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(_defaultCacheTime)
            .SetAbsoluteExpiration(TimeSpan.FromHours(1));

        _cache.Set(DocumentsCacheKey, documents, cacheOptions);
    }

    // Tar bort dokumentlistan från cachen.
    public void InvalidateDocumentsCache()
    {
        _cache.Remove(DocumentsCacheKey);
    }
}