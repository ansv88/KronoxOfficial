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
        // OBS: Den har handlern ateranvands av IHttpClientFactory over flera Blazor
        // Server-kretsar - meddelandehanterarpipelinen (inklusive denna handler)
        // cachas av IHttpClientFactory oberoende av vilken krets som anropar den,
        // och byggs med ett internt scope som INTE ar samma sak som en riktig
        // Blazor-krets DI-scope. Darfor far scopeade tjanster (t.ex.
        // AuthenticationStateProvider) ALDRIG injiceras i den har klassens
        // konstruktor - en sadan instans skulle vara bunden till fel scope och
        // antingen peka pa fel anvandare eller kasta InvalidOperationException.
        //
        // X-User-Roles-headern bakas darfor in i respektive HttpClients
        // DefaultRequestHeaders redan vid konstruktionstillfallet, se
        // ConfigureApiClient i Program.cs. De typade klienterna (DocumentService,
        // CategoryService, CmsService m.fl.) ar registrerade som Scoped via
        // AddHttpClient<T>() och konstrueras darfor korrekt en gang per krets, vid
        // en tidpunkt da HttpContext garanterat ar tillgangligt (under kretsens
        // ursprungliga HTTP-forfragan). Den bakade headern lever sedan kvar for
        // hela kretsens livstid oavsett om HttpContext senare blir null.
        //
        // IHttpContextAccessor anvands har enbart som ett bast-mojligt komplement
        // for anrop som sker under en riktig, pagaende HTTP-forfragan (t.ex.
        // /auth/login-endpointen i Program.cs), dar HttpContext ar tillforlitligt
        // live under hela anropet.
        var context = _httpContextAccessor.HttpContext;

        // Lagg alltid till API-nyckel forst (aven om HttpContext saknas)
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

        if (context != null)
        {
            // Vidarebefordra autentiseringscookie om den finns och inte redan ar satt
            if (context.Request.Cookies.TryGetValue("KronoxAuth", out var authCookie))
            {
                if (!request.Headers.Contains("Cookie"))
                {
                    request.Headers.Add("Cookie", $"KronoxAuth={authCookie}");
                }
            }

            // Lagg till anvandarroller om de finns och inte redan har satts (t.ex.
            // via DefaultRequestHeaders fran ConfigureApiClient i Program.cs).
            var userRoles = context.User?.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            if (userRoles != null && userRoles.Count > 0 && !request.Headers.Contains("X-User-Roles"))
            {
                request.Headers.Add("X-User-Roles", string.Join(",", userRoles));
            }
        }

        if (!request.Headers.Contains("X-User-Roles"))
        {
            _logger.LogDebug(
                "Ingen X-User-Roles header satt via HttpContext for anrop till {Url}. Forlitar sig pa DefaultRequestHeaders. HttpContext finns: {HasContext}",
                request.RequestUri,
                context != null);
        }

        // Utfor anropet
        var response = await base.SendAsync(request, cancellationToken);

        // Logga varning om anropet misslyckas
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Anrop till {Url} misslyckades med statuskod {StatusCode}",
                request.RequestUri, response.StatusCode);

            // Logga headers for felsokning (ej cookies)
            var headers = request.Headers
                .Where(h => !string.Equals(h.Key, "Cookie", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(h => h.Key, h => string.Join(",", h.Value));
            _logger.LogDebug("Headers i anropet: {Headers}",
                JsonSerializer.Serialize(headers, new JsonSerializerOptions { WriteIndented = true }));
        }

        return response;
    }
}
