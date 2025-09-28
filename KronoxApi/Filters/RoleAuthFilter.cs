using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KronoxApi.Filters;

// Filter för att kontrollera om användaren har tillräcklig rollbehörighet för åtkomst.
public class RoleAuthFilter : IAuthorizationFilter
{
    private readonly string[] _requiredRoles;
    private readonly ILogger<RoleAuthFilter> _logger;
    private readonly IConfiguration _configuration;
    private const string RoleHeader = "X-User-Roles";

    public RoleAuthFilter(ILogger<RoleAuthFilter> logger, IConfiguration configuration, string[] requiredRoles)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _requiredRoles = requiredRoles ?? throw new ArgumentNullException(nameof(requiredRoles));
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Kontrollera om användaren har skickat med rollinformation
        if (!context.HttpContext.Request.Headers.TryGetValue(RoleHeader, out var userRolesHeader))
        {
            _logger.LogWarning("Åtkomst nekad: Rollinformation saknas i anrop till {Path} från {IP}",
                context.HttpContext.Request.Path.ToString(),
                context.HttpContext.Connection.RemoteIpAddress);

            context.Result = new ForbidResult();
            return;
        }

        // Hämta betrodda källor från konfigurationen
        var trustedOrigins = _configuration.GetSection("ApiSettings:TrustedOrigins")
            .Get<string[]>() ?? new[] { "https://localhost:7262" }; // Fallback till localhost om inte konfigurerat

        var origin = context.HttpContext.Request.Headers["Origin"].ToString();

        // Kontrollera ursprunget om det finns med i anropet
        if (!string.IsNullOrEmpty(origin) && !trustedOrigins.Contains(origin))
        {
            _logger.LogWarning("Misstänkt anrop från otillåten källa: {Origin}", origin);
            context.Result = new ForbidResult();
            return;
        }

        // Omvandla header-strängen till en lista med roller
        var userRoles = userRolesHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);

        // Kontrollera om användaren har minst en av de nödvändiga rollerna
        if (!_requiredRoles.Any(requiredRole =>
            userRoles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Åtkomst nekad: Användaren har inte rätt roll för {Path}. Användarroller: {Roles}, behöver: {RequiredRoles}",
                context.HttpContext.Request.Path,
                string.Join(", ", userRoles),
                string.Join(", ", _requiredRoles));

            context.Result = new ForbidResult();
            return;
        }

        _logger.LogInformation("Rollkontroll framgångsrik för {Path} med roller: {Roles}",
            context.HttpContext.Request.Path,
            string.Join(", ", userRoles));
    }
}