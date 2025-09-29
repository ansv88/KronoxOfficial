using KronoxFront.DTOs;
using System.Text.Json;

namespace KronoxFront.Middleware;

public class PageAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PageAuthorizationMiddleware> _logger;

    private static readonly HashSet<string> SkippedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "home", "404", "error", "robots.txt", "notfound"
    };

    // Lista över publika sidor som inte kräver inloggning
    private static readonly HashSet<string> PublicPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "omkonsortiet",
        "omsystemet",
        "visioner",
        "kontaktaoss",
        "kontakt",
        "registrera"
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

        if (ShouldSkipPath(path))
        {
            await _next(context);
            return;
        }

        // Kontrollera om det är en publik sida
        if (PublicPages.Contains(path))
        {
            // För publika sidor, tillåt alltid åtkomst
            await _next(context);
            return;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient("KronoxAPI");
            var customPageResponse = await httpClient.GetAsync($"api/custompage/{path}");

            if (customPageResponse.IsSuccessStatusCode)
            {
                var customPageJson = await customPageResponse.Content.ReadAsStringAsync();
                var customPage = JsonSerializer.Deserialize<CustomPageDto>(customPageJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (customPage != null)
                {
                    // Kontrollera om sidan är aktiv
                    if (!customPage.IsActive)
                    {
                        _logger.LogInformation("Custom page {Path} is disabled", path);
                        context.Response.Redirect("/404");
                        return;
                    }

                    // Kontrollera rollbegränsningar
                    if (customPage.RequiredRoles.Any())
                    {
                        var user = context.User;
                        var isAuthenticated = user?.Identity?.IsAuthenticated == true;

                        if (!isAuthenticated || !customPage.RequiredRoles.Any(role => user.IsInRole(role)))
                        {
                            _logger.LogInformation("User {User} lacks access to restricted custom page {Path}",
                                user?.Identity?.Name ?? "Anonymous", path);
                            context.Response.Redirect("/404");
                            return;
                        }
                    }
                }
            }
            else if (customPageResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                // API nekar åtkomst på grund av roller - blockera sidan
                _logger.LogInformation("API denied access to custom page {Path} due to authorization", path);
                context.Response.Redirect("/404");
                return;
            }
            else if (customPageResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Inte en custom page - kontrollera navigation config
                var navResponse = await httpClient.GetAsync($"api/navigation/page/{path}");

                if (navResponse.IsSuccessStatusCode)
                {
                    var json = await navResponse.Content.ReadAsStringAsync();
                    var config = JsonSerializer.Deserialize<NavigationConfigDto>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (config != null && !config.IsActive)
                    {
                        _logger.LogInformation("Page {Path} is disabled", path);
                        context.Response.Redirect("/404");
                        return;
                    }
                }
                else if (navResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.LogInformation("API denied access to navigation for {Path} due to authorization", path);
                    context.Response.Redirect("/404");
                    return;
                }
                // Lägg till: För okända sidor som inte är publika, kräv autentisering
                else if (navResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Om sidan inte finns i navigation config och inte är publik, kräv inloggning
                    var user = context.User;
                    if (!user?.Identity?.IsAuthenticated ?? true)
                    {
                        _logger.LogInformation("Anonymous user tried to access unknown page {Path}, redirecting to 404", path);
                        context.Response.Redirect("/404");
                        return;
                    }
                }
            }
            else
            {
                // Andra HTTP-fel från API
                _logger.LogWarning("Unexpected API response for {Path}: {StatusCode}", path, customPageResponse.StatusCode);

                // Vid oväntat API-fel, blockera åtkomst till okända sidor
                context.Response.Redirect("/404");
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error when checking authorization for {Path}", path);
            // Blockera vid nätverksfel
            context.Response.Redirect("/404");
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when checking authorization for {Path}", path);
            // Blockera vid oväntat fel
            context.Response.Redirect("/404");
            return;
        }

        // Endast om allt är OK - fortsätt till Blazor
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
               path.StartsWith("_vs") ||
               path.Contains("negotiate") ||
               path.Contains('.') ||
               path.StartsWith("images");
    }
}