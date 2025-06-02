using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KronoxApi.Filters;

// Filter f�r att kr�va giltig API-nyckel i varje anrop.
public class ApiKeyAuthFilter : IAuthorizationFilter
{
    private const string ApiKeyHeader = "X-API-Key";

    // Kontrollerar att anropet inneh�ller en giltig API-nyckel.
    // Returnerar 401 om nyckeln saknas eller �r ogiltig, 500 vid konfigurationsfel.
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ApiKeyAuthFilter>>();

        // Kontrollera att API-nyckel finns i headern
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeader, out var extractedApiKey))
        {
            logger.LogWarning("API-nyckel saknas i anrop till {Path} fr�n {IP}",
                context.HttpContext.Request.Path,
                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "ok�nd IP");

            context.Result = new UnauthorizedObjectResult("API-nyckel saknas");
            return;
        }

        // H�mta API-nyckel fr�n konfigurationen
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var apiKey = configuration["ApiSettings:ApiKey"];

        // Kontrollera att API-nyckel �r satt i konfigurationen
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogError("API-nyckel saknas i konfigurationen (ApiSettings:ApiKey)");
            context.Result = new StatusCodeResult(500);
            return;
        }

        // Kontrollera att angiven API-nyckel �r korrekt
        if (!apiKey.Equals(extractedApiKey))
        {
            logger.LogWarning("Ogiltig API-nyckel anv�nd vid �tkomst till {Path} fr�n {IP}",
                context.HttpContext.Request.Path,
                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "ok�nd IP");

            context.Result = new UnauthorizedObjectResult("Ogiltig API-nyckel");
            return;
        }
    }
}