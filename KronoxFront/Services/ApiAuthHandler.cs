using System.Text.Json;

namespace KronoxFront.Services;

public class ApiAuthHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApiAuthHandler> _logger;

    public ApiAuthHandler(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApiAuthHandler> logger)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;

        // Lägg alltid till API-nyckel först (även om HttpContext saknas)
        var apiKey = _configuration["ApiSettings:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            if (!request.Headers.Contains("X-API-Key"))
            {
                request.Headers.Add("X-API-Key", apiKey);
            }
        }
        else
        {
            _logger.LogWarning("Ingen API-nyckel hittades i konfigurationen");
        }

        if (context == null)
        {
            _logger.LogWarning("HttpContext saknas vid anrop till {Url}", request.RequestUri);
            // Även om HttpContext saknas, fortsätt med API-nyckeln
        }
        else
        {
            // Vidarebefordra autentisering via cookie om den finns och inte redan är satt
            if (context.Request.Cookies.TryGetValue("KronoxAuth", out var authCookie))
            {
                if (!request.Headers.Contains("Cookie"))
                {
                    request.Headers.Add("Cookie", $"KronoxAuth={authCookie}");
                }
            }

            // Lägg till användarroller i header om de finns
            var userRoles = context.User?.Claims
                ?.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                ?.Select(c => c.Value)
                ?.ToList();

            if (userRoles != null && userRoles.Any())
            {
                if (!request.Headers.Contains("X-User-Roles"))
                {
                    request.Headers.Add("X-User-Roles", string.Join(",", userRoles));
                }
            }
            else
            {
                _logger.LogWarning("Inga användarroller hittades för anrop till {Url}", request.RequestUri);
            }
        }

        // Utför anropet
        var response = await base.SendAsync(request, cancellationToken);

        // Logga varning om anropet misslyckas
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Anrop till {Url} misslyckades med statuskod {StatusCode}",
                request.RequestUri, response.StatusCode);

            // Logga headers för felsökning (ej cookies)
            var headers = request.Headers
                .Where(h => !string.Equals(h.Key, "Cookie", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(h => h.Key, h => string.Join(",", h.Value));
            _logger.LogDebug("Headers i anropet: {Headers}",
                JsonSerializer.Serialize(headers, new JsonSerializerOptions { WriteIndented = true }));
        }

        return response;
    }
}