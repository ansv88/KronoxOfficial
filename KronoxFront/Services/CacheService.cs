using KronoxFront.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace KronoxFront.Services;

public class CacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    
    // Cache-tider f�r s�kerhet
    private static readonly TimeSpan ShortCache = TimeSpan.FromMinutes(3);    // Auktorisering
    private static readonly TimeSpan MediumCache = TimeSpan.FromMinutes(8);   // Sidinneh�ll
    private static readonly TimeSpan LongCache = TimeSpan.FromMinutes(15);    // Statiskt inneh�ll
    
    // Thread-safe tracking av cache-grupper f�r s�ker invalidering
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _cacheGroups = new();

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    // ============ CORE CACHE FUNCTIONALITY ============
    
    // H�mtar data fr�n cache eller k�r factory-funktion
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
                .SetSize(1); // Hj�lper med memory management

            _cache.Set(key, item, options);
            
            // L�gg till i grupp f�r enkel invalidering
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
    
    // Cache f�r publikt sidinneh�ll (s�kert att dela mellan anv�ndare)
    public async Task<T?> GetPageContentAsync<T>(string pageKey, Func<Task<T?>> factory)
    {
        var cacheKey = $"content_{pageKey}";
        return await GetOrSetAsync(cacheKey, factory, MediumCache, $"page_{pageKey}");
    }

    // Cache f�r navigation (publikt men kan beh�va uppdateras)
    public async Task<List<T>> GetNavigationAsync<T>(string cacheKey, Func<Task<List<T>>> factory)
    {
        return await GetOrSetAsync(cacheKey, factory, MediumCache, "navigation") ?? new List<T>();
    }

    // Cache f�r auktoriseringsdata (kortare tid f�r s�kerhet)
    public async Task<T?> GetAuthDataAsync<T>(string key, Func<Task<T?>> factory)
    {
        return await GetOrSetAsync(key, factory, ShortCache, "auth");
    }

    // Cache f�r action plans (�ndras s�llan)
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

    // Bak�tkompatibilitet
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
        
        // L�gg till i grupp
        _cacheGroups.AddOrUpdate("documents", 
            new ConcurrentBag<string> { "DocumentsWithCategories" },
            (_, existing) => { existing.Add("DocumentsWithCategories"); return existing; });
    }

    // ============ S�KER INVALIDERING ============
    
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

    // Rensa all cache (f�r emergencies)
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
        // Invalidera sj�lva sidans cache
        InvalidatePageCache(pageKey);
        
        // Om det �r kontaktaoss, rensa eventuella gamla kontakt-cacher
        if (pageKey == "kontaktaoss")
        {
            InvalidatePageCache("kontakt"); // F�r s�kerhets skull
            InvalidateKey("content_kontakt");
            InvalidateKey("intro-section-kontakt");
            InvalidateKey("features_public_kontakt");
            InvalidateKey("features_private_kontakt");
            InvalidateKey("faq_kontakt");
        }
        
        _logger.LogInformation("Invalidated caches for {PageKey} and related pages", pageKey);
    }
}