using KronoxApi.Filters;
using Microsoft.AspNetCore.Mvc;

namespace KronoxApi.Attributes;

// Attribut som kräver att användaren har minst en av de angivna rollerna.
// Kontrollerar X-User-Roles headern och verifierar även anropets ursprung mot betrodda källor.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : TypeFilterAttribute
{
    public RequireRoleAttribute(params string[] roles)
        : base(typeof(RoleAuthFilter))
    {
        Arguments = new object[] { roles };
    }
}