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
        try
        {
            var items = await _context.NavigationConfigs.AsNoTracking()
                .OrderBy(n => n.SortOrder)
                .ThenBy(n => n.DisplayName)
                .ToListAsync();

            _logger.LogDebug("Hämtade {Count} NavigationConfigs.", items.Count);
            return Ok(items.ToDtos());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av NavigationConfigs.");
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av navigation.");
        }
    }

    // Skapa navigation config (NavigationSettings när ingen fanns)
    [HttpPost]
    [RequireRole("Admin")]
    public async Task<ActionResult<NavigationConfigDto>> Create([FromBody] NavigationConfigDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Ogiltig modell vid skapande av NavigationConfig. Fel: {ErrorCount}", ModelState.ErrorCount);
            return BadRequest(ModelState);
        }

        try
        {
            var exists = await _context.NavigationConfigs.AnyAsync(n => n.PageKey == dto.PageKey);
            if (exists)
            {
                _logger.LogWarning("NavigationConfig för pageKey={PageKey} finns redan.", dto.PageKey);
                return Conflict(new { message = "NavigationConfig för denna PageKey finns redan." });
            }

            // Förhindra att home skapas via UI
            if (string.Equals(dto.PageKey, "home", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Försök att skapa system-sidan 'home' via UI nekades.");
                return BadRequest("Startsidan är en system-sida och kan inte skapas via UI.");
            }

            // Validera role-specific
            if (string.Equals(dto.ItemType, "role-specific", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(dto.RequiredRoles))
            {
                _logger.LogWarning("RequiredRoles saknas för role-specific sida vid skapande. pageKey={PageKey}", dto.PageKey);
                return BadRequest("RequiredRoles måste anges för role-specific sidor (t.ex. Admin,Styrelse).");
            }

            var model = dto.ToModel();
            model.Id = 0;
            model.CreatedAt = DateTime.UtcNow;
            model.LastModified = DateTime.UtcNow;
            model.RequiredRoles = string.IsNullOrWhiteSpace(dto.RequiredRoles) ? null : dto.RequiredRoles.Trim();

            _context.NavigationConfigs.Add(model);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Skapade NavigationConfig id={Id}, pageKey={PageKey}.", model.Id, model.PageKey);
            return CreatedAtAction(nameof(GetByPageKey), new { pageKey = model.PageKey }, model.ToDto());
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Databasfel vid skapande av NavigationConfig pageKey={PageKey}.", dto?.PageKey);
            return StatusCode(500, "Ett fel inträffade vid lagring av navigation.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oväntat fel vid skapande av NavigationConfig pageKey={PageKey}.", dto?.PageKey);
            return StatusCode(500, "Ett oväntat fel inträffade vid skapande av navigation.");
        }
    }

    // Uppdatera navigation config
    [HttpPut("{id:int}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] NavigationUpdateDto update)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Ogiltig modell vid uppdatering av NavigationConfig id={Id}. Fel: {ErrorCount}", id, ModelState.ErrorCount);
            return BadRequest(ModelState);
        }

        try
        {
            var item = await _context.NavigationConfigs.FirstOrDefaultAsync(n => n.Id == id);
            if (item == null)
            {
                _logger.LogWarning("NavigationConfig id={Id} hittades inte för uppdatering.", id);
                return NotFound();
            }

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

                _logger.LogDebug("Uppdaterade system-sidan 'home' (id={Id}).", id);
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
            {
                _logger.LogWarning("RequiredRoles saknas för role-specific sida vid uppdatering. id={Id}, pageKey={PageKey}", id, item.PageKey);
                return BadRequest("RequiredRoles måste anges för role-specific sidor (t.ex. Admin,Styrelse).");
            }

            item.DisplayName        = update.DisplayName;
            item.SortOrder          = update.SortOrder;
            item.GuestSortOrder     = update.GuestSortOrder;
            item.MemberSortOrder    = update.MemberSortOrder;
            item.IsVisibleToGuests  = update.IsVisibleToGuests;
            item.IsVisibleToMembers = update.IsVisibleToMembers;
            item.IsActive           = update.IsActive;
            item.RequiredRoles      = string.IsNullOrWhiteSpace(update.RequiredRoles) ? null : update.RequiredRoles.Trim();
            item.LastModified       = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogDebug("Uppdaterade NavigationConfig id={Id}, pageKey={PageKey}.", id, item.PageKey);
            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Samtidighetskonflikt vid uppdatering av NavigationConfig id={Id}.", id);
            return Conflict(new { message = "Objektet uppdaterades av en annan process. Ladda om och försök igen." });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Databasfel vid uppdatering av NavigationConfig id={Id}.", id);
            return StatusCode(500, "Ett fel inträffade vid uppdatering av navigation.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oväntat fel vid uppdatering av NavigationConfig id={Id}.", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid uppdatering av navigation.");
        }
    }

    // Hämta en navigation config per pageKey (NavigationSettings)
    [HttpGet("page/{pageKey}")]
    public async Task<ActionResult<NavigationConfigDto>> GetByPageKey(string pageKey)
    {
        try
        {
            var item = await _context.NavigationConfigs.AsNoTracking()
                .FirstOrDefaultAsync(n => n.PageKey == pageKey);

            if (item == null)
            {
                if (string.Equals(pageKey, "home", StringComparison.OrdinalIgnoreCase))
                {
                    // Defensiv default för home
                    _logger.LogDebug("Ingen NavigationConfig för 'home' – returnerar defensiv default.");
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

            _logger.LogDebug("NavigationConfig för pageKey={PageKey} hämtad.", pageKey);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av NavigationConfig för pageKey={PageKey}.", pageKey);
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av navigation.");
        }
    }
}