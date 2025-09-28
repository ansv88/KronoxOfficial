using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Cryptography;
using System.Text;

namespace KronoxApi.Filters;

// Filter för att kräva giltig API-nyckel i varje anrop.
public class ApiKeyAuthFilter : IAuthorizationFilter
{
    private const string ApiKeyHeader = "X-API-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ApiKeyAuthFilter>>();
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        // Hämta API-nyckel från konfigurationen
        var expectedApiKey = configuration["ApiSettings:ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            logger.LogError("API-nyckel saknas i konfigurationen (ApiSettings:ApiKey)");
            context.Result = new ObjectResult(new { message = "Serverfel: API-nyckel saknas i konfigurationen." }) { StatusCode = 500 };
            return;
        }

        // Kontrollera att API-nyckel finns i headern
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeader, out var extracted) ||
            string.IsNullOrWhiteSpace(extracted))
        {
            logger.LogWarning("API-nyckel saknas i anrop till {Path} från {IP}",
                context.HttpContext.Request.Path,
                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "okänd IP");

            context.Result = new UnauthorizedObjectResult(new { message = "API-nyckel saknas." });
            return;
        }

        var provided = extracted.ToString().Trim();

        // Säker jämförelse (konstant tid)
        if (!FixedTimeEquals(Encoding.UTF8.GetBytes(expectedApiKey), Encoding.UTF8.GetBytes(provided)))
        {
            logger.LogWarning("Ogiltig API-nyckel använd vid åtkomst till {Path} från {IP}",
                context.HttpContext.Request.Path,
                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "okänd IP");

            context.Result = new UnauthorizedObjectResult(new { message = "Ogiltig API-nyckel." });
            return;
        }

        // OK – släpp igenom
    }

    private static bool FixedTimeEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        // Använd CryptographicOperations för att undvika timing-attacker.
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}