using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KronoxApi.Filters;

// Filter f�r att kontrollera om anv�ndaren har tillr�cklig rollbeh�righet f�r �tkomst.
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
        // Kontrollera om anv�ndaren har skickat med rollinformation
        if (!context.HttpContext.Request.Headers.TryGetValue(RoleHeader, out var userRolesHeader))
        {
            _logger.LogWarning("�tkomst nekad: Rollinformation saknas i anrop till {Path} fr�n {IP}",
                context.HttpContext.Request.Path.ToString(),
                context.HttpContext.Connection.RemoteIpAddress);

            context.Result = new ForbidResult();
            return;
        }

        // H�mta betrodda k�llor fr�n konfigurationen
        var trustedOrigins = _configuration.GetSection("ApiSettings:TrustedOrigins")
            .Get<string[]>() ?? new[] { "https://localhost:7262" }; // Fallback till localhost om inte konfigurerat

        var origin = context.HttpContext.Request.Headers["Origin"].ToString();

        // Kontrollera ursprunget om det finns med i anropet
        if (!string.IsNullOrEmpty(origin) && !trustedOrigins.Contains(origin))
        {
            _logger.LogWarning("Misst�nkt anrop fr�n otill�ten k�lla: {Origin}", origin);
            context.Result = new ForbidResult();
            return;
        }

        // Omvandla header-str�ngen till en lista med roller
        var userRoles = userRolesHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);

        // Kontrollera om anv�ndaren har minst en av de n�dv�ndiga rollerna
        if (!_requiredRoles.Any(requiredRole =>
            userRoles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("�tkomst nekad: Anv�ndaren har inte r�tt roll f�r {Path}. Anv�ndarroller: {Roles}, beh�ver: {RequiredRoles}",
                context.HttpContext.Request.Path,
                string.Join(", ", userRoles),
                string.Join(", ", _requiredRoles));

            context.Result = new ForbidResult();
            return;
        }

        _logger.LogInformation("Rollkontroll framg�ngsrik f�r {Path} med roller: {Roles}",
            context.HttpContext.Request.Path,
            string.Join(", ", userRoles));
    }
}