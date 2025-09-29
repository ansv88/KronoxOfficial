using System.ComponentModel.DataAnnotations;

namespace KronoxFront.Validators;

public static class PageKeyValidator
{
    // Komplett lista �ver reserverade routes
    private static readonly string[] ReservedRoutes =
    {
        // Admin-specifika routes (kolliderar med /admin/{PageKey})
        "pages", "users", "news", "memberlogos", "suggestions",
        "documents", "manage", "dashboard", "home",
        
        // Befintliga publik sidor (kolliderar med /{PageKey})  
        "omkonsortiet", "omsystemet", "visioner", "kontakt", "kontaktaoss",
        "dokument", "forvaltning", "medlemsnytt", "for-styrelsen", "for-vnsg", "registrera",
        
        // Tekniska routes
        "api", "auth", "login", "logout", "error", "account",
        "admin", "static", "images", "css", "js", "lib",
        
        // Vanliga systemord (f�rhindra konflikter)
        "index", "about", "contact", "help", "support",
        "profile", "settings", "config", "test", "demo"
    };

    private static readonly string[] ReservedPrefixes =
    {
        "admin", "api", "auth", "for-"
    };

    public static ValidationResult? ValidatePageKey(string pageKey, ValidationContext context)
    {
        if (string.IsNullOrEmpty(pageKey))
            return ValidationResult.Success; // Hanteras av Required-attributet

        var lowerPageKey = pageKey.ToLower();

        // Kontrollera reserverade routes
        if (ReservedRoutes.Contains(lowerPageKey))
        {
            return new ValidationResult($"PageKey '{pageKey}' �r reserverat f�r systemfunktioner. V�lj ett annat namn.");
        }

        // Kontrollera reserverade prefix
        if (ReservedPrefixes.Any(prefix => lowerPageKey.StartsWith(prefix)))
        {
            var conflictingPrefix = ReservedPrefixes.First(prefix => lowerPageKey.StartsWith(prefix));
            return new ValidationResult($"PageKey '{pageKey}' f�r inte b�rja med '{conflictingPrefix}'. V�lj ett annat namn.");
        }

        return ValidationResult.Success;
    }

    /// <summary>
    /// Kontrollera om en PageKey �r tillg�nglig utan att kasta exception
    /// </summary>
    public static bool IsPageKeyAvailable(string pageKey)
    {
        if (string.IsNullOrEmpty(pageKey))
            return false;

        var lowerPageKey = pageKey.ToLower();
        return !ReservedRoutes.Contains(lowerPageKey) &&
               !ReservedPrefixes.Any(prefix => lowerPageKey.StartsWith(prefix));
    }

    /// <summary>
    /// F�resl� alternativa PageKeys om den angivna �r reserverad
    /// </summary>
    public static List<string> SuggestAlternatives(string pageKey)
    {
        if (IsPageKeyAvailable(pageKey))
            return new List<string>();

        var suggestions = new List<string>
        {
            $"{pageKey}-sida",
            $"min-{pageKey}",
            $"{pageKey}-info",
            $"om-{pageKey}",
            $"{pageKey}-2"
        };

        return suggestions.Where(IsPageKeyAvailable).ToList();
    }
}