using KronoxApi.Attributes;
using KronoxApi.Data;
using KronoxApi.DTOs;
using KronoxApi.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Controllers;

/// <summary>
/// API‑kontroller för navigationskonfigurationer.
/// Publik läsning av navigation; Admin kan skapa/uppdatera, omordna samt auto‑registrera
/// navigation för anpassade sidor. __EnableRateLimiting("API")__.
/// </summary>

[ApiController]
[Route("api/[controller]")]
[RequireApiKey]
[EnableRateLimiting("API")]
public class NavigationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NavigationController> _logger;

    public NavigationController(ApplicationDbContext context, ILogger<NavigationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Hämta alla navigation configs (NavMenu, Footer, NavigationAdmin)
    [HttpGet]
    public async Task<ActionResult<List<NavigationConfigDto>>> GetAll()
    {
        var items = await _context.NavigationConfigs.AsNoTracking()
            .OrderBy(n => n.SortOrder)
            .ThenBy(n => n.DisplayName)
            .ToListAsync();

        return Ok(items.ToDtos());
    }

    // Skapa navigation config (NavigationSettings när ingen fanns)
    [HttpPost]
    [RequireRole("Admin")]
    public async Task<ActionResult<NavigationConfigDto>> Create([FromBody] NavigationConfigDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var exists = await _context.NavigationConfigs.AnyAsync(n => n.PageKey == dto.PageKey);
        if (exists) return Conflict(new { message = "NavigationConfig för denna PageKey finns redan." });

        // Förhindra att home skapas via UI
        if (string.Equals(dto.PageKey, "home", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Startsidan är en system-sida och kan inte skapas via UI.");

        // Validera role-specific
        if (string.Equals(dto.ItemType, "role-specific", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(dto.RequiredRoles))
            return BadRequest("RequiredRoles måste anges för role-specific sidor (t.ex. Admin,Styrelse).");

        var model = dto.ToModel();
        model.Id = 0;
        model.CreatedAt = DateTime.UtcNow;
        model.LastModified = DateTime.UtcNow;
        model.RequiredRoles = string.IsNullOrWhiteSpace(dto.RequiredRoles) ? null : dto.RequiredRoles.Trim();

        _context.NavigationConfigs.Add(model);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByPageKey), new { pageKey = model.PageKey }, model.ToDto());
    }

    // Uppdatera navigation config
    [HttpPut("{id:int}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] NavigationUpdateDto update)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var item = await _context.NavigationConfigs.FirstOrDefaultAsync(n => n.Id == id);
        if (item == null) return NotFound();

        // home: tillåt bara namn/ordning men tvinga fast flaggor
        if (string.Equals(item.PageKey, "home", StringComparison.OrdinalIgnoreCase))
        {
            // Tillåt ändra namn/sortering, men tvinga policyflaggor för home
            item.DisplayName        = update.DisplayName;
            item.SortOrder          = update.SortOrder;
            item.GuestSortOrder     = update.GuestSortOrder;
            item.MemberSortOrder    = update.MemberSortOrder;

            item.IsActive           = true;
            item.IsVisibleToGuests  = true;
            item.IsVisibleToMembers = true;
            item.IsSystemItem       = true;
            item.RequiredRoles      = null;

            item.LastModified       = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Skydda systemposter från att bli "icke-system"
        if (new[] { "admin", "logout" }.Contains(item.PageKey, StringComparer.OrdinalIgnoreCase))
        {
            item.IsSystemItem = true;
        }

        // Validera role-specific
        if (string.Equals(item.ItemType, "role-specific", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(update.RequiredRoles))
            return BadRequest("RequiredRoles måste anges för role-specific sidor (t.ex. Admin,Styrelse).");

        item.DisplayName       = update.DisplayName;
        item.SortOrder         = update.SortOrder;
        item.GuestSortOrder    = update.GuestSortOrder;
        item.MemberSortOrder   = update.MemberSortOrder;
        item.IsVisibleToGuests = update.IsVisibleToGuests;
        item.IsVisibleToMembers= update.IsVisibleToMembers;
        item.IsActive          = update.IsActive;
        item.RequiredRoles     = string.IsNullOrWhiteSpace(update.RequiredRoles) ? null : update.RequiredRoles.Trim();
        item.LastModified      = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Hämta en navigation config per pageKey (NavigationSettings)
    [HttpGet("page/{pageKey}")]
    public async Task<ActionResult<NavigationConfigDto>> GetByPageKey(string pageKey)
    {
        var item = await _context.NavigationConfigs.AsNoTracking()
            .FirstOrDefaultAsync(n => n.PageKey == pageKey);

        if (item == null)
        {
            if (string.Equals(pageKey, "home", StringComparison.OrdinalIgnoreCase))
            {
                // Defensiv default för home
                return Ok(new NavigationConfigDto
                {
                    PageKey = "home",
                    DisplayName = "Startsida",
                    ItemType = "static",
                    SortOrder = 0,
                    IsVisibleToGuests = true,
                    IsVisibleToMembers = true,
                    IsActive = true,
                    IsSystemItem = true,
                    RequiredRoles = null,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                });
            }
            return NotFound();
        }

        var dto = item.ToDto();

        // Normalisera home i svaret
        if (string.Equals(dto.PageKey, "home", StringComparison.OrdinalIgnoreCase))
        {
            dto.IsActive = true;
            dto.IsVisibleToGuests = true;
            dto.IsVisibleToMembers = true;
            dto.IsSystemItem = true;
            dto.RequiredRoles = null;
        }

        return Ok(dto);
    }
}