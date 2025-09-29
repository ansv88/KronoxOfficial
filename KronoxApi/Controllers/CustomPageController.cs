using KronoxApi.Attributes;
using KronoxApi.Data;
using KronoxApi.DTOs;
using KronoxApi.Extensions;
using KronoxApi.Models;
using KronoxApi.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Controllers;

/// <summary>
/// API‑kontroller för anpassade sidor (CustomPages) och relaterad navigation.
/// Admin kan skapa/uppdatera/radera sidor; publika endpoints för att läsa navigation.
/// Skapar ContentBlock vid sid‑skapande och synkar __NavigationConfig__ för huvudsidor.
/// </summary>

[ApiController]
[Route("api/[controller]")]
[RequireApiKey]
[EnableRateLimiting("API")]
public class CustomPageController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomPageController> _logger;

    public CustomPageController(ApplicationDbContext context, ILogger<CustomPageController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [RequireRole("Admin")]
    public async Task<ActionResult<List<CustomPageDto>>> GetCustomPages()
    {
        try
        {
            var pages = await _context.CustomPages
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.DisplayName)
                .ToListAsync();

            return Ok(pages.Select(p => p.ToDto()).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av anpassade sidor");
            return StatusCode(500, "Ett fel uppstod vid hämtning av anpassade sidor");
        }
    }

    [HttpGet("navigation")]
    public async Task<ActionResult<List<NavigationPageDto>>> GetNavigationPages()
    {
        try
        {
            // Hämta ev. NavigationConfig för att kunna styra statiska sidor
            var navConfigs = await _context.NavigationConfigs.AsNoTracking().ToListAsync();

            // Endast aktiva custom pages som ska visas i navigationen
            var pages = await _context.CustomPages
                .Where(p => p.IsActive && p.ShowInNavigation)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.DisplayName)
                .ToListAsync();

            var mainPages = pages.Where(p => string.IsNullOrEmpty(p.ParentPageKey)).ToList();
            var navigationPages = new List<NavigationPageDto>();

            foreach (var mainPage in mainPages)
            {
                var navPage = new NavigationPageDto
                {
                    PageKey = mainPage.PageKey,
                    DisplayName = mainPage.DisplayName,
                    NavigationType = mainPage.NavigationType,
                    ParentPageKey = mainPage.ParentPageKey,
                    SortOrder = mainPage.SortOrder,
                    RequiredRoles = mainPage.RequiredRoles
                };

                var childPages = pages
                    .Where(p => p.ParentPageKey == mainPage.PageKey)
                    .OrderBy(p => p.SortOrder)
                    .Select(child => new NavigationPageDto
                    {
                        PageKey = child.PageKey,
                        DisplayName = child.DisplayName,
                        NavigationType = child.NavigationType,
                        ParentPageKey = child.ParentPageKey,
                        SortOrder = child.SortOrder,
                        RequiredRoles = child.RequiredRoles
                    }).ToList();

                navPage.Children = childPages;
                navigationPages.Add(navPage);
            }

            var staticPages = new List<string> { 
                "home", "omkonsortiet", "omsystemet", "visioner", "kontaktaoss",
                "dokument", "forvaltning", "medlemsnytt", "forstyrelsen", "forvnsg"
            };

            foreach (var staticPageKey in staticPages)
            {
                var cfg = navConfigs.FirstOrDefault(n => n.PageKey == staticPageKey);

                // hoppa över om explicit inaktiv
                if (cfg is { IsActive: false })
                    continue;

                // roller för statisk sida hämtas från NavigationConfig (CSV -> lista)
                var requiredRolesForStatic = (cfg?.RequiredRoles ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

                var children = pages
                    .Where(p => p.ParentPageKey == staticPageKey)
                    .OrderBy(p => p.SortOrder)
                    .Select(child => new NavigationPageDto
                    {
                        PageKey = child.PageKey,
                        DisplayName = child.DisplayName,
                        NavigationType = child.NavigationType,
                        ParentPageKey = child.ParentPageKey,
                        SortOrder = child.SortOrder,
                        RequiredRoles = child.RequiredRoles
                    }).ToList();

                // ta med statisk sida endast om den har barn eller det finns en NavigationConfig
                if (children.Any() || cfg != null)
                {
                    navigationPages.Add(new NavigationPageDto
                    {
                        PageKey = staticPageKey,
                        DisplayName = cfg?.DisplayName ?? GetStaticPageDisplayName(staticPageKey),
                        NavigationType = "main",
                        ParentPageKey = null,
                        SortOrder = cfg?.SortOrder ?? GetStaticPageSortOrder(staticPageKey),
                        RequiredRoles = requiredRolesForStatic,
                        Children = children
                    });
                }
            }

            return Ok(navigationPages.OrderBy(p => p.SortOrder).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av navigationssidor");
            return StatusCode(500, "Ett fel uppstod vid hämtning av navigationssidor");
        }
    }

    private string GetStaticPageDisplayName(string pageKey) => pageKey switch
    {
        "home" => "Startsida",
        "omkonsortiet" => "Om konsortiet",
        "omsystemet" => "Om systemet",
        "visioner" => "Visioner & verksamhetsidé",
        "kontaktaoss" => "Kontakta oss",
        "dokument" => "Dokument",
        "forvaltning" => "Förvaltning",
        "medlemsnytt" => "Medlemsnytt",
        _ => pageKey
    };

    private int GetStaticPageSortOrder(string pageKey) => pageKey switch
    {
        "home" => 0,
        "omkonsortiet" => 1,
        "omsystemet" => 2,
        "visioner" => 3,
        "kontaktaoss" => 17,
        "dokument" => 10,
        "forvaltning" => 11,
        "medlemsnytt" => 5,
        _ => 99
    };

    [HttpGet("{pageKey}")]
    public async Task<ActionResult<CustomPageDto>> GetCustomPage(string pageKey)
    {
        try
        {
            var page = await _context.CustomPages
                .FirstOrDefaultAsync(p => p.PageKey == pageKey);

            if (page == null) return NotFound();

            return Ok(page.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av anpassad sida {PageKey}", pageKey);
            return StatusCode(500, "Ett fel uppstod vid hämtning av anpassad sida");
        }
    }

    [HttpPost]
    [RequireRole("Admin")]
    public async Task<ActionResult<CustomPageDto>> CreateCustomPage([FromBody] CreateCustomPageRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var existingPage = await _context.CustomPages
                .FirstOrDefaultAsync(p => p.PageKey == request.PageKey);

            if (existingPage != null)
            {
                return BadRequest("En sida med denna PageKey finns redan");
            }

            var customPage = new CustomPage
            {
                PageKey = request.PageKey,
                Title = request.Title,
                DisplayName = request.DisplayName,
                Description = request.Description,
                IsActive = request.IsActive,
                ShowInNavigation = request.ShowInNavigation,
                NavigationType = request.NavigationType,
                ParentPageKey = request.ParentPageKey,
                SortOrder = request.SortOrder,
                RequiredRoles = request.RequiredRoles,
                CreatedBy = User.Identity?.Name ?? "Admin"
            };

            _context.CustomPages.Add(customPage);
            await _context.SaveChangesAsync();

            var contentBlock = new ContentBlock
            {
                PageKey = request.PageKey,
                Title = request.Title,
                HtmlContent = "<p>Välkommen till denna nya sida. Redigera innehållet via adminpanelen.</p>",
                LastModified = DateTime.UtcNow
            };

            _context.ContentBlocks.Add(contentBlock);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Anpassad sida skapad: {PageKey} av {User}", request.PageKey, User.Identity?.Name);

            if (request.ShowInNavigation && request.NavigationType == "main")
            {
                var navigationConfig = new NavigationConfig
                {
                    PageKey = customPage.PageKey,
                    DisplayName = customPage.DisplayName,
                    ItemType = "custom",
                    SortOrder = customPage.SortOrder,
                    IsVisibleToGuests = !customPage.RequiredRoles.Any(),
                    IsVisibleToMembers = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    RequiredRoles = customPage.RequiredRoles.Any()
                        ? string.Join(",", customPage.RequiredRoles)
                        : null
                };

                _context.NavigationConfigs.Add(navigationConfig);
                await _context.SaveChangesAsync();
            }

            return Ok(customPage.ToDto());
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Samtidighetskonflikt vid skapande av anpassad sida {PageKey}", request.PageKey);
            return Conflict(new { message = "Innehållet uppdaterades av någon annan. Försök igen." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid skapande av anpassad sida");
            return StatusCode(500, "Ett fel uppstod vid skapande av anpassad sida");
        }
    }

    [HttpPut("{pageKey}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateCustomPage(string pageKey, [FromBody] UpdateCustomPageRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var page = await _context.CustomPages
                .FirstOrDefaultAsync(p => p.PageKey == pageKey);

            if (page == null)
            {
                return NotFound();
            }

            page.Title = request.Title;
            page.DisplayName = request.DisplayName;
            page.Description = request.Description;
            page.IsActive = request.IsActive;
            page.ShowInNavigation = request.ShowInNavigation;
            page.NavigationType = request.NavigationType;

            // Konsistens – endast dropdown får ha ParentPageKey
            page.ParentPageKey = request.NavigationType == "dropdown"
                ? request.ParentPageKey
                : null;

            page.SortOrder = request.SortOrder;
            page.RequiredRoles = request.RequiredRoles;
            page.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var existingNavConfig = await _context.NavigationConfigs
                .FirstOrDefaultAsync(n => n.PageKey == pageKey);

            if (request.ShowInNavigation && request.NavigationType == "main")
            {
                if (existingNavConfig != null)
                {
                    existingNavConfig.DisplayName = request.DisplayName;
                    existingNavConfig.SortOrder = request.SortOrder;
                    existingNavConfig.IsVisibleToGuests = !request.RequiredRoles.Any();
                    existingNavConfig.IsActive = request.IsActive;
                    existingNavConfig.LastModified = DateTime.UtcNow;
                    existingNavConfig.RequiredRoles = request.RequiredRoles.Any()
                        ? string.Join(",", request.RequiredRoles)
                        : null;
                }
                else
                {
                    var navigationConfig = new NavigationConfig
                    {
                        PageKey = page.PageKey,
                        DisplayName = page.DisplayName,
                        ItemType = "custom",
                        SortOrder = page.SortOrder,
                        IsVisibleToGuests = !page.RequiredRoles.Any(),
                        IsVisibleToMembers = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        RequiredRoles = page.RequiredRoles.Any()
                            ? string.Join(",", page.RequiredRoles)
                            : null
                    };

                    _context.NavigationConfigs.Add(navigationConfig);
                }
            }
            else
            {
                if (existingNavConfig != null)
                {
                    _context.NavigationConfigs.Remove(existingNavConfig);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogDebug("Anpassad sida uppdaterad: {PageKey} av {User}", pageKey, User.Identity?.Name);

            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Samtidighetskonflikt vid uppdatering av anpassad sida {PageKey}", pageKey);
            return Conflict(new { message = "Innehållet uppdaterades av någon annan. Ladda om sidan och försök igen." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av anpassad sida {PageKey}", pageKey);
            return StatusCode(500, "Ett fel uppstod vid uppdatering av anpassad sida");
        }
    }

    [HttpDelete("{pageKey}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> DeleteCustomPage(string pageKey)
    {
        try
        {
            var page = await _context.CustomPages
                .FirstOrDefaultAsync(p => p.PageKey == pageKey);

            if (page == null)
            {
                return NotFound();
            }

            var navigationConfig = await _context.NavigationConfigs
                .FirstOrDefaultAsync(n => n.PageKey == pageKey);
            if (navigationConfig != null)
            {
                _context.NavigationConfigs.Remove(navigationConfig);
            }

            var contentBlock = await _context.ContentBlocks
                .FirstOrDefaultAsync(cb => cb.PageKey == pageKey);
            if (contentBlock != null)
            {
                _context.ContentBlocks.Remove(contentBlock);
            }

            var featureSections = await _context.FeatureSections
                .Where(fs => fs.PageKey == pageKey)
                .ToListAsync();
            _context.FeatureSections.RemoveRange(featureSections);

            var faqSections = await _context.FaqSections
                .Where(fs => fs.PageKey == pageKey)
                .ToListAsync();
            _context.FaqSections.RemoveRange(faqSections);

            _context.CustomPages.Remove(page);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Anpassad sida borttagen: {PageKey} av {User}", pageKey, User.Identity?.Name);

            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Samtidighetskonflikt vid borttagning av anpassad sida {PageKey}", pageKey);
            return Conflict(new { message = "Innehållet uppdaterades av någon annan. Försök igen." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av anpassad sida {PageKey}", pageKey);
            return StatusCode(500, "Ett fel uppstod vid borttagning av anpassad sida");
        }
    }
}