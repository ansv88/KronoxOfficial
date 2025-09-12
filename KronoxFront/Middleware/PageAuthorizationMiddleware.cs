using KronoxFront.DTOs;
using System.Text.Json;

namespace KronoxFront.Middleware;

public class PageAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PageAuthorizationMiddleware> _logger;

    // HashSet för prestanda - snabbare lookup än List
    private static readonly HashSet<string> SkippedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "home", "404", "error", "robots.txt"
    };

    public PageAuthorizationMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory, ILogger<PageAuthorizationMiddleware> logger)
    {
        _next = next;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.TrimStart('/').ToLower() ?? "";
        
        // Skippa vissa typer av requests
        if (ShouldSkipPath(path))
        {
            await _next(context);
            return;
        }

        try
        {
            // Kontrollera om sidan är aktiv i navigationen
            var httpClient = _httpClientFactory.CreateClient("KronoxAPI");
            var response = await httpClient.GetAsync($"api/navigation/page/{path}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var config = JsonSerializer.Deserialize<NavigationConfigDto>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                if (config != null && !config.IsActive)
                {
                    _logger.LogInformation("Page {Path} is disabled, redirecting to 404", path);
                    
                    // Omdirigera till 404 om sidan är inaktiverad
                    context.Response.Redirect("/404");
                    return;
                }
            }
            // Om API-anropet returnerar 404 betyder det att sidan inte finns i navigation configs
            // vilket är OK - inte alla sidor behöver finnas där (t.ex. custom pages)
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check page authorization for {Path}", path);
            // Fail-safe: tillåt åtkomst om kontrollen misslyckas
        }

        await _next(context);
    }
    
    private static bool ShouldSkipPath(string path)
    {
        return string.IsNullOrEmpty(path) ||
               SkippedPaths.Contains(path) ||
               path.StartsWith("admin") ||
               path.StartsWith("api") ||
               path.StartsWith("auth") ||
               path.StartsWith("_framework") ||
               path.StartsWith("_blazor") ||
               path.Contains('.') || // Statiska filer (.css, .js, .png, etc.)
               path.StartsWith("images");
    }
}