using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Cryptography;
using System.Text;

namespace KronoxApi.Filters;

// Filter f�r att kr�va giltig API-nyckel i varje anrop.
public class ApiKeyAuthFilter : IAuthorizationFilter
{
    private const string ApiKeyHeader = "X-API-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ApiKeyAuthFilter>>();
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        // H�mta API-nyckel fr�n konfigurationen
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
            logger.LogWarning("API-nyckel saknas i anrop till {Path} fr�n {IP}",
                context.HttpContext.Request.Path,
                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "ok�nd IP");

            context.Result = new UnauthorizedObjectResult(new { message = "API-nyckel saknas." });
            return;
        }

        var provided = extracted.ToString().Trim();

        // S�ker j�mf�relse (konstant tid)
        if (!FixedTimeEquals(Encoding.UTF8.GetBytes(expectedApiKey), Encoding.UTF8.GetBytes(provided)))
        {
            logger.LogWarning("Ogiltig API-nyckel anv�nd vid �tkomst till {Path} fr�n {IP}",
                context.HttpContext.Request.Path,
                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "ok�nd IP");

            context.Result = new UnauthorizedObjectResult(new { message = "Ogiltig API-nyckel." });
            return;
        }

        // OK � sl�pp igenom
    }

    private static bool FixedTimeEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        // Anv�nd CryptographicOperations f�r att undvika timing-attacker.
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}