using KronoxFront.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace KronoxFront.Services;

public class CacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    
    // Cache-tider för säkerhet
    private static readonly TimeSpan ShortCache = TimeSpan.FromMinutes(3);    // Auktorisering
    private static readonly TimeSpan MediumCache = TimeSpan.FromMinutes(8);   // Sidinnehåll
    private static readonly TimeSpan LongCache = TimeSpan.FromMinutes(15);    // Statiskt innehåll
    
    // Thread-safe tracking av cache-grupper för säker invalidering
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _cacheGroups = new();

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    // ============ CORE CACHE FUNCTIONALITY ============
    
    // Hämtar data från cache eller kör factory-funktion
    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan expiration, string? group = null)
    {
        if (_cache.TryGetValue(key, out T? cachedItem))
        {
            _logger.LogDebug("Cache hit: {CacheKey}", key);
            return cachedItem;
        }

        _logger.LogDebug("Cache miss: {CacheKey}", key);
        var item = await factory();
        
        if (item != null)
        {
            var options = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(expiration)
                .SetAbsoluteExpiration(expiration.Multiply(2)) // Max 2x sliding time
                .SetPriority(CacheItemPriority.Normal)
                .SetSize(1); // Hjälper med memory management

            _cache.Set(key, item, options);
            
            // Lägg till i grupp för enkel invalidering
            if (!string.IsNullOrEmpty(group))
            {
                _cacheGroups.AddOrUpdate(group, 
                    new ConcurrentBag<string> { key },
                    (_, existing) => { existing.Add(key); return existing; });
            }
            
            _logger.LogDebug("Cached: {CacheKey} for {Expiration}", key, expiration);
        }

        return item;
    }

    // ============ SPECIFIKA CACHE-METODER ============
    
    // Cache för publikt sidinnehåll (säkert att dela mellan användare)
    public async Task<T?> GetPageContentAsync<T>(string pageKey, Func<Task<T?>> factory)
    {
        var cacheKey = $"content_{pageKey}";
        return await GetOrSetAsync(cacheKey, factory, MediumCache, $"page_{pageKey}");
    }

    // Cache för navigation (publikt men kan behöva uppdateras)
    public async Task<List<T>> GetNavigationAsync<T>(string cacheKey, Func<Task<List<T>>> factory)
    {
        return await GetOrSetAsync(cacheKey, factory, MediumCache, "navigation") ?? new List<T>();
    }

    // Cache för auktoriseringsdata (kortare tid för säkerhet)
    public async Task<T?> GetAuthDataAsync<T>(string key, Func<Task<T?>> factory)
    {
        return await GetOrSetAsync(key, factory, ShortCache, "auth");
    }

    // Cache för action plans (ändras sällan)
    public async Task<T?> GetActionPlanAsync<T>(string pageKey, Func<Task<T?>> factory)
    {
        var cacheKey = $"actionplan_{pageKey}";
        return await GetOrSetAsync(cacheKey, factory, LongCache, "actionplans");
    }

    // ============ DOKUMENTFUNKTIONALITET ============
    
    public async Task<List<DocumentViewModel>?> GetDocumentsAsync(Func<Task<List<DocumentViewModel>?>> factory)
    {
        return await GetOrSetAsync("DocumentsWithCategories", factory, LongCache, "documents");
    }

    // Bakåtkompatibilitet
    public List<DocumentViewModel>? GetDocumentsFromCache()
    {
        return _cache.TryGetValue("DocumentsWithCategories", out List<DocumentViewModel>? documents) ? documents : null;
    }

    public void SetDocumentsToCache(List<DocumentViewModel> documents)
    {
        var options = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(LongCache)
            .SetAbsoluteExpiration(TimeSpan.FromHours(1))
            .SetSize(documents.Count);

        _cache.Set("DocumentsWithCategories", documents, options);
        
        // Lägg till i grupp
        _cacheGroups.AddOrUpdate("documents", 
            new ConcurrentBag<string> { "DocumentsWithCategories" },
            (_, existing) => { existing.Add("DocumentsWithCategories"); return existing; });
    }

    // ============ SÄKER INVALIDERING ============
    
    // Invalidera alla cache-poster i en grupp
    public void InvalidateGroup(string group)
    {
        if (_cacheGroups.TryRemove(group, out var keys))
        {
            foreach (var key in keys)
            {
                _cache.Remove(key);
            }
            _logger.LogDebug("Invalidated cache group: {Group} ({Count} keys)", group, keys.Count());
        }
    }

    // Invalidera specifik cache-post
    public void InvalidateKey(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Invalidated cache key: {Key}", key);
    }

    // Invalidera all sidrelaterad cache
    public void InvalidatePageCache(string? pageKey = null)
    {
        if (!string.IsNullOrEmpty(pageKey))
        {
            InvalidateGroup($"page_{pageKey}");
        }
        else
        {
            InvalidateGroup("content");
            InvalidateGroup("navigation");
        }
    }

    public void InvalidateDocumentsCache()
    {
        InvalidateKey("DocumentsWithCategories");
        InvalidateGroup("documents");
    }

    // Rensa all cache (för emergencies)
    public void ClearAll()
    {
        if (_cache is MemoryCache mc)
        {
            mc.Compact(1.0); // Tvinga bort allt
        }
        _cacheGroups.Clear();
        _logger.LogWarning("All cache cleared");
    }

    // Invalidera cacher relaterade till specifik sida
    public void InvalidateRelatedPageCaches(string pageKey)
    {
        // Invalidera själva sidans cache
        InvalidatePageCache(pageKey);
        
        // Om det är kontaktaoss, rensa eventuella gamla kontakt-cacher
        if (pageKey == "kontaktaoss")
        {
            InvalidatePageCache("kontakt"); // För säkerhets skull
            InvalidateKey("content_kontakt");
            InvalidateKey("intro-section-kontakt");
            InvalidateKey("features_public_kontakt");
            InvalidateKey("features_private_kontakt");
            InvalidateKey("faq_kontakt");
        }
        
        _logger.LogInformation("Invalidated caches for {PageKey} and related pages", pageKey);
    }
}