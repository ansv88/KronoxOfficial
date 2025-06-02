using KronoxApi.Filters;
using Microsoft.AspNetCore.Mvc;

namespace KronoxApi.Attributes;

// Attribut som kr�ver att f�rfr�gan inneh�ller en giltig API-nyckel i X-API-Key headern.
// Nyckeln valideras mot v�rdet i konfigurationen (ApiSettings:ApiKey).
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireApiKeyAttribute : TypeFilterAttribute
{
    public RequireApiKeyAttribute() : base(typeof(ApiKeyAuthFilter))
    {
    }
}