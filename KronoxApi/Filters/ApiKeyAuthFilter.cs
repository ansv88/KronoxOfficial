using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KronoxApi.Filters;

// Filter för att kräva giltig API-nyckel i varje anrop.
public class ApiKeyAuthFilter : IAuthorizationFilter
{
    private const string ApiKeyHeader = "X-API-Key";

    // Kontrollerar att anropet innehåller en giltig API-nyckel.
    // Returnerar 401 om nyckeln saknas eller är ogiltig, 500 vid konfigurationsfel.
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ApiKeyAuthFilter>>();

        // Kontrollera att API-nyckel finns i headern
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeader, out var extractedApiKey))
        {
            logger.LogWarning("API-nyckel saknas i anrop till {Path} från {IP}",
                context.HttpContext.Request.Path,
                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "okänd IP");

            context.Result = new UnauthorizedObjectResult("API-nyckel saknas");
            return;
        }

        // Hämta API-nyckel från konfigurationen
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var apiKey = configuration["ApiSettings:ApiKey"];

        // Kontrollera att API-nyckel är satt i konfigurationen
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogError("API-nyckel saknas i konfigurationen (ApiSettings:ApiKey)");
            context.Result = new StatusCodeResult(500);
            return;
        }

        // Kontrollera att angiven API-nyckel är korrekt
        if (!apiKey.Equals(extractedApiKey))
        {
            logger.LogWarning("Ogiltig API-nyckel använd vid åtkomst till {Path} från {IP}",
                context.HttpContext.Request.Path,
                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "okänd IP");

            context.Result = new UnauthorizedObjectResult("Ogiltig API-nyckel");
            return;
        }
    }
}