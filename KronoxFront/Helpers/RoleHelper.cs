namespace KronoxFront.Helpers;

public static class RoleHelper
{
    public static readonly Dictionary<string, string> RoleDisplayNames = new()
    {
        { "Admin", "Administratör" },
        { "Styrelse", "Styrelsen" },
        { "Medlem", "Medlemmar" },
        { "Ny användare", "Nya användare" }
    };

    public static string GetDisplayName(string role)
    {
        return RoleDisplayNames.TryGetValue(role, out var displayName) ? displayName : role;
    }

    public static List<string> GetAllRoles()
    {
        return RoleDisplayNames.Keys.ToList();
    }
}