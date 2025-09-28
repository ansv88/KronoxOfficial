using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using KronoxApi.Data;

namespace KronoxApi.Services;

public interface IRoleValidationService
{
    // Validerar att rollerna existerar i systemet.
    Task<bool> ValidateRolesAsync(IEnumerable<string> roles);

    // Returnerar alla giltiga rollnamn.
    Task<List<string>> GetValidRolesAsync();

    // Kontrollerar om anv�ndarroller ger �tkomst till given kategori.
    Task<bool> UserHasAccessToCategoryAsync(int categoryId, string[] userRoles);

    // Returnerar id:n f�r kategorier som anv�ndarroller har �tkomst till.
    Task<List<int>> GetAccessibleCategoryIdsAsync(string[] userRoles);
}

public class RoleValidationService : IRoleValidationService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoleValidationService> _logger;

    public RoleValidationService(
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        ILogger<RoleValidationService> logger)
    {
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ValidateRolesAsync(IEnumerable<string> roles)
    {
        try
        {
            var validRoles = await GetValidRolesAsync();
            return roles.All(role => validRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid validering av roller");
            return false;
        }
    }

    public async Task<List<string>> GetValidRolesAsync()
    {
        try
        {
            return await _roleManager.Roles
                .Where(r => r.Name != null)
                .Select(r => r.Name!)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid h�mtning av giltiga roller");
            return new List<string>();
        }
    }

    public async Task<bool> UserHasAccessToCategoryAsync(int categoryId, string[] userRoles)
    {
        try
        {
            // Admin har alltid tillg�ng
            if (userRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
                return true;

            var category = await _context.MainCategories.FindAsync(categoryId);
            if (category == null || !category.IsActive) return false;

            // Om ingen specifik roll kr�vs, till�t "Medlem" som default
            if (!category.AllowedRoles.Any())
                return userRoles.Contains("Medlem", StringComparer.OrdinalIgnoreCase);

            return category.AllowedRoles.Any(role =>
                userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid kontroll av kategori�tkomst f�r kategori {CategoryId}", categoryId);
            return false;
        }
    }

    public async Task<List<int>> GetAccessibleCategoryIdsAsync(string[] userRoles)
    {
        try
        {
            var categories = await _context.MainCategories
                .Where(mc => mc.IsActive)
                .ToListAsync();

            var accessibleIds = new List<int>();

            foreach (var category in categories)
            {
                // Admin kan se alla kategorier
                if (userRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
                {
                    accessibleIds.Add(category.Id);
                    continue;
                }

                // Om ingen specifik roll kr�vs, till�t "Medlem" som default
                if (!category.AllowedRoles.Any())
                {
                    if (userRoles.Contains("Medlem", StringComparer.OrdinalIgnoreCase))
                        accessibleIds.Add(category.Id);
                    continue;
                }

                // Kontrollera om anv�ndaren har n�gon av de till�tna rollerna
                if (category.AllowedRoles.Any(role =>
                    userRoles.Contains(role, StringComparer.OrdinalIgnoreCase)))
                {
                    accessibleIds.Add(category.Id);
                }
            }

            return accessibleIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid h�mtning av tillg�ngliga kategorier");
            return new List<int>();
        }
    }
}