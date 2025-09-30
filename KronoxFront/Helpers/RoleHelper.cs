namespace KronoxFront.Helpers;

public static class RoleHelper
{
    // Skiftl�gesok�nslig uppslagning
    public static readonly Dictionary<string, string> RoleDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Admin", "Administrat�r" },
        { "Styrelse", "Styrelsen" },
        { "Medlem", "Medlemmar" },
        { "Ny anv�ndare", "Nya anv�ndare" },
        { "RequiresAuthentication", "Inloggade anv�ndare" },
        { "ExcludeNewUser", "Verifierade anv�ndare" }
    };

    public static string GetDisplayName(string role)
    {
        var key = role ?? string.Empty;
        if (RoleDisplayNames.TryGetValue(key, out var displayName) && !string.IsNullOrEmpty(displayName))
            return displayName;

        return key;
    }

    public static bool TryGetDisplayName(string role, out string displayName)
    {
        var key = role ?? string.Empty;
        if (RoleDisplayNames.TryGetValue(key, out var name) && !string.IsNullOrEmpty(name))
        {
            displayName = name;
            return true;
        }

        displayName = key;
        return false;
    }

    public static bool IsKnownRole(string role)
        => RoleDisplayNames.ContainsKey(role ?? string.Empty);

    public static List<string> GetAllRoles()
        => RoleDisplayNames.Keys.ToList();
}