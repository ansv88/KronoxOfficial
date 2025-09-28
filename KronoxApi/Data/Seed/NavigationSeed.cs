using KronoxApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace KronoxApi.Data.Seed;

public static class NavigationSeed
{
    /// <summary>
    /// Seedar standardnavigationsposter (statiska/system/rollspecifika).
    /// K�rs endast om tabellen �r tom (idempotent).
    /// </summary>

    public static async Task SeedNavigationConfigAsync(ApplicationDbContext context)
    {
        // F�rs�k h�mta logger via DbContext-service provider (om tillg�ngligt)
        var logger = context.GetService<ILogger<Program>>();

        if (await context.NavigationConfigs.AnyAsync())
        {
            logger?.LogDebug("NavigationConfigs inneh�ller redan data. Hoppar �ver seeding.");
            return;
        }

        logger?.LogDebug("Navigation seeding startar...");

        var configs = new List<NavigationConfig>
        {
            // STATISKA SIDOR
            new() { PageKey = "omkonsortiet", DisplayName = "Om konsortiet", ItemType = "static", 
                   SortOrder = 1, IsVisibleToGuests = true, IsVisibleToMembers = true, IsSystemItem = false },
            new() { PageKey = "omsystemet", DisplayName = "Om systemet", ItemType = "static", 
                   SortOrder = 2, IsVisibleToGuests = true, IsVisibleToMembers = false, IsSystemItem = false },
            new() { PageKey = "visioner", DisplayName = "Visioner & verksamhetsid�", ItemType = "static", 
                   SortOrder = 3, IsVisibleToGuests = true, IsVisibleToMembers = false, IsSystemItem = false },
            
            // MEDLEMSSIDOR
            new() { PageKey = "medlemsnytt", DisplayName = "Medlemsnytt", ItemType = "system", 
                   SortOrder = 5, IsVisibleToGuests = false, IsVisibleToMembers = true, IsSystemItem = false },
            new() { PageKey = "forvaltning", DisplayName = "F�rvaltning", ItemType = "system", 
                   SortOrder = 10, IsVisibleToGuests = false, IsVisibleToMembers = true, IsSystemItem = false },
            
            // ROLLSPECIFIKA SIDOR
            new() { PageKey = "forstyrelsen", DisplayName = "F�r styrelsen", ItemType = "role-specific", 
                   SortOrder = 12, IsVisibleToGuests = false, IsVisibleToMembers = true, IsSystemItem = false,
                   RequiredRoles = "Admin,Styrelse" },
            new() { PageKey = "forvnsg", DisplayName = "F�r VNSG", ItemType = "role-specific", 
                   SortOrder = 13, IsVisibleToGuests = false, IsVisibleToMembers = true, IsSystemItem = false,
                   RequiredRoles = "Admin,Styrelse" },
            
            new() { PageKey = "dokument", DisplayName = "Dokument", ItemType = "system", 
                   SortOrder = 15, IsVisibleToGuests = false, IsVisibleToMembers = true, IsSystemItem = false },
            new() { PageKey = "kontakt", DisplayName = "Kontakta oss", ItemType = "static", 
                   SortOrder = 17, IsVisibleToGuests = true, IsVisibleToMembers = true, IsSystemItem = false },
            
            // SYSTEM-L�NKAR (l�sta)
            new() { PageKey = "admin", DisplayName = "Admin", ItemType = "system", 
                   SortOrder = 19, IsVisibleToGuests = false, IsVisibleToMembers = true, IsSystemItem = true,
                   RequiredRoles = "Admin" },
            new() { PageKey = "logout", DisplayName = "Logga ut", ItemType = "system", 
                   SortOrder = 20, IsVisibleToGuests = false, IsVisibleToMembers = true, IsSystemItem = true }
        };

        context.NavigationConfigs.AddRange(configs);
        await context.SaveChangesAsync();

        logger?.LogDebug("Navigation seeding klar. Antal poster: {Count}", configs.Count);
    }
}