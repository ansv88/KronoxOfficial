using KronoxApi.Filters;
using Microsoft.AspNetCore.Mvc;

namespace KronoxApi.Attributes;

// Attribut som kr�ver att anv�ndaren har minst en av de angivna rollerna.
// Kontrollerar X-User-Roles headern och verifierar �ven anropets ursprung mot betrodda k�llor.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : TypeFilterAttribute
{
    public RequireRoleAttribute(params string[] roles)
        : base(typeof(RoleAuthFilter))
    {
        Arguments = new object[] { roles };
    }
}