//using KronoxApi.Models;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;

//namespace KronoxApi.Data.Seed;

//// Innehåller metoder för att seeda grundläggande data vid applikationsstart.
//public static class StartupSeed
//{
//    // Konfigurerar och seedar användardata och kategorier för applikationen.
//    public static async Task SeedAllAsync(this IServiceProvider serviceProvider, IConfiguration configuration)
//    {
//        using var scope = serviceProvider.CreateScope();
//        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
//        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

//        // Seeda roller
//        string[] roles = { "Admin", "Styrelse", "Medlem", "Ny användare" };
//        foreach (var role in roles)
//        {
//            if (!await roleManager.RoleExistsAsync(role))
//            {
//                await roleManager.CreateAsync(new IdentityRole(role));
//            }
//        }

//        // Kolla om en admin-användare redan finns
//        var adminUser = await userManager.FindByNameAsync("admin");
//        if (adminUser == null)
//        {
//            // Skapa en ny admin-användare
//            var user = new ApplicationUser
//            {
//                UserName = "admin",
//                Email = "admin@admin.se",
//                FirstName = "Super",
//                LastName = "Admin",
//                Academy = "KronoX"
//            };

//            // Hämta lösenord från User Secrets
//            var adminPwd = configuration["Admin:Password"] ?? throw new Exception("Saknar Admin:Password i konfigurationen");
//            var result = await userManager.CreateAsync(user, adminPwd!);

//            if (!result.Succeeded)
//            {
//                throw new Exception("Kunde inte skapa admin-användare: " +
//                    string.Join(", ", result.Errors.Select(e => e.Description)));
//            }
//            await userManager.AddToRoleAsync(user, "Admin");
//        }


//        // Seeda kategorier om inget finns
//        if (!await dbContext.MainCategories.AnyAsync())
//        {
//            // Huvudkategorier
//            var mainCategories = new List<MainCategory>
//            {
//                new() { Name = "Användarträffar"},
//                new() { Name = "Fokusträffar" },
//                new() { Name = "Dokument" },
//                new() { Name = "Övrigt" },
//                new() { Name = "Styrelsen" },
//                new() { Name = "Tekniska dokument" },
//                new() { Name = "VNSG" },
//                new() { Name = "Stämmor" }
//            };
//            dbContext.MainCategories.AddRange(mainCategories);
//            await dbContext.SaveChangesAsync();

//            // Underkategorier
//            var subCategories = new List<SubCategory>()
//            {
//                new() { Name = "Versionshistorik" },
//                new() { Name = "Arbetsmöten" },
//                new() { Name = "Anvisningar" },
//                new() { Name = "Månadsbrev" },
//                new() { Name = "Protokoll" },
//                new() { Name = "Avtal" },
//                new() { Name = "Anteckningar" },
//                new() { Name = "Budget" },
//                new() { Name = "Utfall" }
//            };
//            dbContext.SubCategories.AddRange(subCategories);
//            await dbContext.SaveChangesAsync();
//        }
//    }
//}

using KronoxApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Data.Seed;

// Innehåller metoder för att seeda grundläggande data vid applikationsstart.
public static class StartupSeed
{
    // Konfigurerar och seedar användardata och kategorier för applikationen.
    public static async Task SeedAllAsync(this IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Seeda roller
            await SeedRolesAsync(roleManager, logger);

            // Seeda admin-användare
            await SeedAdminUserAsync(userManager, configuration, logger);

            // Seeda kategorier om de inte redan finns
            await SeedCategoriesAsync(dbContext, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ett fel inträffade i SeedAllAsync");
            throw;
        }
    }

    // Skapar standardroller i systemet om de inte redan finns
    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        logger.LogInformation("Kontrollerar grundläggande roller...");

        string[] roles = { "Admin", "Styrelse", "Medlem", "Ny användare" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                logger.LogInformation("Skapar roll: {Role}", role);
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    // Skapar en admin-användare om den inte redan finns
    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        logger.LogInformation("Kontrollerar admin-användare...");

        // Kolla om en admin-användare redan finns
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser != null)
        {
            logger.LogInformation("Admin-användare finns redan");
            return;
        }

        // Skapa en ny admin-användare
        var user = new ApplicationUser
        {
            UserName = "admin",
            Email = "admin@admin.se",
            FirstName = "Super",
            LastName = "Admin",
            Academy = "KronoX"
        };

        // Hämta lösenord från konfiguration
        var adminPwd = configuration["Admin:Password"];
        if (string.IsNullOrEmpty(adminPwd))
        {
            logger.LogError("Saknar Admin:Password i konfigurationen");
            throw new InvalidOperationException("Saknar Admin:Password i konfigurationen");
        }

        logger.LogInformation("Skapar admin-användare...");
        var result = await userManager.CreateAsync(user, adminPwd);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Kunde inte skapa admin-användare: {Errors}", errors);
            throw new InvalidOperationException($"Kunde inte skapa admin-användare: {errors}");
        }

        await userManager.AddToRoleAsync(user, "Admin");
        logger.LogInformation("Admin-användare skapad framgångsrikt");
    }

    // Seedar huvud- och underkategorier för dokumenthanteringssystemet
    private static async Task SeedCategoriesAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        logger.LogInformation("Kontrollerar om kategorier behöver seedas...");

        if (await dbContext.MainCategories.AnyAsync())
        {
            logger.LogInformation("Kategorier finns redan i databasen");
            return;
        }

        logger.LogInformation("Skapar huvudkategorier...");
        var mainCategories = GetDefaultMainCategories().Select(name => new MainCategory { Name = name }).ToList();
        dbContext.MainCategories.AddRange(mainCategories);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Skapar underkategorier...");
        var subCategories = GetDefaultSubCategories().Select(name => new SubCategory { Name = name }).ToList();
        dbContext.SubCategories.AddRange(subCategories);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Kategorier seedade framgångsrikt");
    }

    // Returnerar en lista med standardnamn för huvudkategorier
    private static IEnumerable<string> GetDefaultMainCategories()
    {
        return new[]
        {
            "Användarträffar",
            "Fokusträffar",
            "Dokument",
            "Övrigt",
            "Styrelsen",
            "Tekniska dokument",
            "VNSG",
            "Stämmor"
        };
    }

    // Returnerar en lista med standardnamn för underkategorier
    private static IEnumerable<string> GetDefaultSubCategories()
    {
        return new[]
        {
            "Versionshistorik",
            "Arbetsmöten",
            "Anvisningar",
            "Månadsbrev",
            "Protokoll",
            "Avtal",
            "Anteckningar",
            "Budget",
            "Utfall"
        };
    }
}