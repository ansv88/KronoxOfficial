using KronoxApi.Attributes;
using KronoxApi.Data;
using KronoxApi.DTOs;
using KronoxApi.Models;
using KronoxApi.Requests;
using KronoxApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Controllers;

/// <summary>
/// API-kontroller för hantering av huvud- och underkategorier.
/// Stöder rollbaserad åtkomst för kategorier.
/// </summary>

[ApiController]
[Route("api/[controller]")]
[RequireApiKey]
public class CategoryController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRoleValidationService _roleValidationService;
    private readonly ILogger<CategoryController> _log;

    public CategoryController(
        ApplicationDbContext dbContext,
        IRoleValidationService roleValidationService,
        ILogger<CategoryController> log)
    {
        _dbContext = dbContext;
        _roleValidationService = roleValidationService;
        _log = log;
    }

    // Hämtar alla huvudkategorier (endast admin).
    [HttpGet("main")]
    [RequireRole("Admin")]
    public async Task<IActionResult> ListAllMainCategories()
    {
        try
        {
            var categories = await _dbContext.MainCategories
                .Where(mc => mc.IsActive)
                .Select(mc => new MainCategoryDto
                {
                    Id = mc.Id,
                    Name = mc.Name,
                    AllowedRoles = mc.AllowedRoles,
                    IsActive = mc.IsActive
                })
                .ToListAsync();

            return Ok(categories);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid hämtning av huvudkategorier");
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av kategorier.");
        }
    }

    // **NYCKELMETHOD** - Hämtar kategorier baserat på användarens roller
    [HttpGet("main/accessible")]
    [RequireRole("Admin", "Styrelse", "Medlem")]
    public async Task<IActionResult> GetAccessibleMainCategories()
    {
        try
        {
            var userRoles = Request.Headers["X-User-Roles"].ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries);

            var accessibleCategoryIds = await _roleValidationService.GetAccessibleCategoryIdsAsync(userRoles);

            var categories = await _dbContext.MainCategories
                .Where(mc => accessibleCategoryIds.Contains(mc.Id))
                .Select(mc => new MainCategoryDto
                {
                    Id = mc.Id,
                    Name = mc.Name,
                    AllowedRoles = mc.AllowedRoles,
                    IsActive = mc.IsActive
                })
                .ToListAsync();

            return Ok(categories);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid hämtning av tillgängliga kategorier");
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av kategorier.");
        }
    }

    // Hämtar alla underkategorier.
    [HttpGet("sub")]
    [RequireRole("Admin")]
    public async Task<IActionResult> ListAllSubCategories()
    {
        try
        {
            var allSubCategories = await _dbContext.SubCategories
                .Select(c => new SubCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            return Ok(allSubCategories);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid hämtning av underkategorier");
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av underkategorier.");
        }
    }

    // Hämtar en huvudkategori baserat på ID.
    [HttpGet("main/{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> GetMainCategory(int id)
    {
        try
        {
            var mainCategory = await _dbContext.MainCategories.FindAsync(id);
            if (mainCategory == null)
            {
                _log.LogWarning("Huvudkategorin med ID {Id} hittades inte.", id);
                return NotFound("Kategorin hittades inte.");
            }

            var dto = new MainCategoryDto
            {
                Id = mainCategory.Id,
                Name = mainCategory.Name,
                AllowedRoles = mainCategory.AllowedRoles,
                IsActive = mainCategory.IsActive
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ett fel inträffade vid hämtning av huvudkategorin med ID {Id}.", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av kategorin.");
        }
    }

    // Hämtar en underkategori baserat på ID.
    [HttpGet("sub/{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> GetSubCategory(int id)
    {
        try
        {
            var subCategory = await _dbContext.SubCategories.FindAsync(id);
            if (subCategory == null)
            {
                _log.LogWarning("Underkategorin med ID {Id} hittades inte.", id);
                return NotFound("Kategorin hittades inte.");
            }

            var dto = new SubCategoryDto
            {
                Id = subCategory.Id,
                Name = subCategory.Name
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ett fel inträffade vid hämtning av underkategorin med ID {Id}.", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av kategorin.");
        }
    }

    // Skapar en ny huvudkategori med rollbaserad åtkomst.
    [HttpPost("main")]
    [RequireRole("Admin")]
    public async Task<IActionResult> CreateMainCategory([FromBody] CreateMainCategoryRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Validera att rollerna existerar
            if (request.AllowedRoles.Any() && !await _roleValidationService.ValidateRolesAsync(request.AllowedRoles))
            {
                return BadRequest("En eller flera angivna roller existerar inte.");
            }

            var mainCategory = new MainCategory
            {
                Name = request.Name,
                AllowedRoles = request.AllowedRoles,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.MainCategories.Add(mainCategory);
            await _dbContext.SaveChangesAsync();

            var dto = new MainCategoryDto
            {
                Id = mainCategory.Id,
                Name = mainCategory.Name,
                AllowedRoles = mainCategory.AllowedRoles,
                IsActive = mainCategory.IsActive
            };

            _log.LogDebug("Huvudkategori '{Name}' skapad med roller: {Roles}",
                mainCategory.Name, string.Join(", ", mainCategory.AllowedRoles));

            return CreatedAtAction(nameof(GetMainCategory), new { id = mainCategory.Id }, dto);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ett fel inträffade vid skapande av huvudkategori.");
            return StatusCode(500, "Ett oväntat fel inträffade vid skapande av huvudkategori.");
        }
    }

    // Skapar en ny underkategori.
    [HttpPost("sub")]
    [RequireRole("Admin")]
    public async Task<IActionResult> CreateSubCategory([FromBody] CreateSubCategoryRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var subCategory = new SubCategory
            {
                Name = request.Name
            };

            _dbContext.SubCategories.Add(subCategory);
            await _dbContext.SaveChangesAsync();

            var dto = new SubCategoryDto
            {
                Id = subCategory.Id,
                Name = subCategory.Name
            };

            _log.LogDebug("Underkategori '{Name}' skapad", subCategory.Name);

            return CreatedAtAction(nameof(GetSubCategory), new { id = subCategory.Id }, dto);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ett fel inträffade vid skapande av underkategori.");
            return StatusCode(500, "Ett oväntat fel inträffade vid skapande av underkategori.");
        }
    }

    // Uppdaterar en huvudkategori.
    [HttpPut("main/{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateMainCategory(int id, [FromBody] UpdateMainCategoryRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var mainCategory = await _dbContext.MainCategories.FindAsync(id);
            if (mainCategory == null)
            {
                _log.LogWarning("Huvudkategorin med ID {Id} hittades inte för uppdatering.", id);
                return NotFound("Kategorin hittades inte.");
            }

            // Validera att rollerna existerar
            if (request.AllowedRoles.Any() && !await _roleValidationService.ValidateRolesAsync(request.AllowedRoles))
            {
                return BadRequest("En eller flera angivna roller existerar inte.");
            }

            mainCategory.Name = request.Name;
            mainCategory.AllowedRoles = request.AllowedRoles;
            mainCategory.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _log.LogDebug("Huvudkategori '{Name}' uppdaterad med roller: {Roles}",
                mainCategory.Name, string.Join(", ", mainCategory.AllowedRoles));

            return Ok();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ett fel inträffade vid uppdatering av huvudkategorin med ID {Id}.", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid uppdatering av kategorin.");
        }
    }

    // Uppdaterar en underkategori.
    [HttpPut("sub/{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateSubCategory(int id, [FromBody] UpdateSubCategoryRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var subCategory = await _dbContext.SubCategories.FindAsync(id);
            if (subCategory == null)
            {
                _log.LogWarning("Underkategorin med ID {Id} hittades inte för uppdatering.", id);
                return NotFound("Underkategorin hittades inte.");
            }

            subCategory.Name = request.Name;
            await _dbContext.SaveChangesAsync();

            _log.LogDebug("Underkategori '{Name}' uppdaterad", subCategory.Name);

            return Ok();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ett fel inträffade vid uppdatering av underkategorin med ID {Id}.", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid uppdatering av underkategorin.");
        }
    }

    // Tar bort en huvudkategori (soft delete).
    [HttpDelete("main/{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> DeleteMainCategory(int id)
    {
        try
        {
            var mainCategory = await _dbContext.MainCategories.FindAsync(id);
            if (mainCategory == null)
            {
                _log.LogWarning("Huvudkategorin med ID {Id} hittades inte för borttagning.", id);
                return NotFound("Huvudkategorin hittades inte.");
            }

            // Kontrollera att inga aktiva dokument använder denna kategori
            var activeDocuments = await _dbContext.Documents
                .Where(d => d.MainCategoryId == id && !d.IsArchived)
                .CountAsync();

            if (activeDocuments > 0)
            {
                _log.LogWarning("Försök att ta bort huvudkategori {Id} som används av {Count} aktiva dokument",
                    id, activeDocuments);
                return BadRequest($"Kategorin används av {activeDocuments} aktiva dokument och kan inte tas bort.");
            }

            // Soft delete - markera som inaktiv
            mainCategory.IsActive = false;
            mainCategory.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _log.LogDebug("Huvudkategori '{Name}' inaktiverad", mainCategory.Name);

            return Ok($"Huvudkategori '{mainCategory.Name}' har inaktiverats.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ett fel inträffade vid borttagning av huvudkategorin med ID {Id}.", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid borttagning av huvudkategorin.");
        }
    }

    // Tar bort en underkategori.
    [HttpDelete("sub/{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> DeleteSubCategory(int id)
    {
        try
        {
            var subCategory = await _dbContext.SubCategories.FindAsync(id);
            if (subCategory == null)
            {
                _log.LogWarning("Underkategorin med ID {Id} hittades inte för borttagning.", id);
                return NotFound("Underkategorin hittades inte.");
            }

            // Kontrollera att inga dokument använder denna underkategori
            var documentsUsingCategory = await _dbContext.Documents
                .Where(d => d.SubCategories.Contains(id))
                .CountAsync();

            if (documentsUsingCategory > 0)
            {
                _log.LogWarning("Försök att ta bort underkategori {Id} som används av {Count} dokument",
                    id, documentsUsingCategory);
                return BadRequest($"Underkategorin används av {documentsUsingCategory} dokument och kan inte tas bort.");
            }

            _dbContext.SubCategories.Remove(subCategory);
            await _dbContext.SaveChangesAsync();

            _log.LogDebug("Underkategori '{Name}' borttagen", subCategory.Name);

            return Ok($"Underkategori '{subCategory.Name}' har tagits bort.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ett fel inträffade vid borttagning av underkategorin med ID {Id}.", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid borttagning av underkategorin.");
        }
    }
}