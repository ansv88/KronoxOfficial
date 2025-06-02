using KronoxApi.Filters;
using Microsoft.AspNetCore.Mvc;

namespace KronoxApi.Attributes;

// Attribut som kräver att förfrågan innehåller en giltig API-nyckel i X-API-Key headern.
// Nyckeln valideras mot värdet i konfigurationen (ApiSettings:ApiKey).
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireApiKeyAttribute : TypeFilterAttribute
{
    public RequireApiKeyAttribute() : base(typeof(ApiKeyAuthFilter))
    {
    }
}