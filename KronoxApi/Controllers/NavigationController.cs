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

    // Hämta en navigation config per pageKey (NavigationSettings)
    [HttpGet("page/{pageKey}")]
    public async Task<ActionResult<NavigationConfigDto>> GetByPageKey(string pageKey)
    {
        var item = await _context.NavigationConfigs.AsNoTracking()
            .FirstOrDefaultAsync(n => n.PageKey == pageKey);

        if (item == null) return NotFound();
        return Ok(item.ToDto());
    }

    // Skapa navigation config (NavigationSettings när ingen fanns)
    [HttpPost]
    [RequireRole("Admin")]
    public async Task<ActionResult<NavigationConfigDto>> Create([FromBody] NavigationConfigDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Säkerställ unik pageKey
        var exists = await _context.NavigationConfigs.AnyAsync(n => n.PageKey == dto.PageKey);
        if (exists) return Conflict(new { message = "NavigationConfig för denna PageKey finns redan." });

        var model = dto.ToModel();
        model.Id = 0;
        model.CreatedAt = DateTime.UtcNow;
        model.LastModified = DateTime.UtcNow;

        _context.NavigationConfigs.Add(model);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByPageKey), new { pageKey = model.PageKey }, model.ToDto());
    }

    // Uppdatera navigation config (NavigationSettings, NavigationAdmin)
    [HttpPut("{id:int}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] NavigationUpdateDto update)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var item = await _context.NavigationConfigs.FirstOrDefaultAsync(n => n.Id == id);
        if (item == null) return NotFound();

        item.DisplayName = update.DisplayName;
        item.SortOrder = update.SortOrder;
        item.GuestSortOrder = update.GuestSortOrder;
        item.MemberSortOrder = update.MemberSortOrder;
        item.IsVisibleToGuests = update.IsVisibleToGuests;
        item.IsVisibleToMembers = update.IsVisibleToMembers;
        item.IsActive = update.IsActive;
        item.RequiredRoles = update.RequiredRoles;
        item.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}