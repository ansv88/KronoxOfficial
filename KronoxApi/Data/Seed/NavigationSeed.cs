using KronoxApi.Data;
using KronoxApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Data.Seed;

public static class NavigationSeed
{
    public static async Task SeedNavigationConfigAsync(ApplicationDbContext context)
    {
        if (await context.NavigationConfigs.AnyAsync()) return;

        var configs = new List<NavigationConfig>
        {
            // STATISKA SIDOR (f�r b�da g�ster och medlemmar)
            new() { PageKey = "omkonsortiet", DisplayName = "Om konsortiet", ItemType = "static", 
                   SortOrder = 1, IsVisibleToGuests = true, IsVisibleToMembers = true, IsSystemItem = false },
            new() { PageKey = "omsystemet", DisplayName = "Om systemet", ItemType = "static", 
                   SortOrder = 2, IsVisibleToGuests = true, IsVisibleToMembers = false, IsSystemItem = false },
            new() { PageKey = "visioner", DisplayName = "Visioner & verksamhetsid�", ItemType = "static", 
                   SortOrder = 3, IsVisibleToGuests = true, IsVisibleToMembers = false, IsSystemItem = false },
            
            // SYSTEM-SIDOR (endast f�r medlemmar)
            new() { PageKey = "medlemsnytt", DisplayName = "Medlemsnytt", ItemType = "system", 
                   SortOrder = 5, IsVisibleToGuests = false, IsVisibleToMembers = true, IsSystemItem = false },
            new() { PageKey = "forvaltning", DisplayName = "F�rvaltning", ItemType = "system", 
                   SortOrder = 10, IsVisibleToGuests = false, IsVisibleToMembers = true, IsSystemItem = false },
            new() { PageKey = "dokument", DisplayName = "Dokument", ItemType = "system", 
                   SortOrder = 15, IsVisibleToGuests = false, IsVisibleToMembers = true, IsSystemItem = false },
            
            // STYRELSE-SIDOR F�RE KONTAKT
            new() { PageKey = "for-styrelsen", DisplayName = "F�r styrelsen", ItemType = "system", 
                   SortOrder = 16, IsVisibleToGuests = false, IsVisibleToMembers = true, IsSystemItem = false },
            
            // KONTAKT SIST F�RE ADMIN
            new() { PageKey = "kontakt", DisplayName = "Kontakta oss", ItemType = "static", 
                   SortOrder = 17, IsVisibleToGuests = true, IsVisibleToMembers = true, IsSystemItem = false },
            
            // SKYDDADE SYSTEM-L�NKAR (sist)
            new() { PageKey = "admin", DisplayName = "Admin", ItemType = "system", 
                   SortOrder = 19, IsVisibleToGuests = false, IsVisibleToMembers = true, IsSystemItem = true },
            new() { PageKey = "logout", DisplayName = "Logga ut", ItemType = "system", 
                   SortOrder = 20, IsVisibleToGuests = false, IsVisibleToMembers = true, IsSystemItem = true }
        };

        context.NavigationConfigs.AddRange(configs);
        await context.SaveChangesAsync();
    }
}