using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KronoxApi.Data;
using KronoxApi.Models;
using KronoxApi.DTOs;
using KronoxApi.Requests;
using KronoxApi.Attributes;
using Microsoft.AspNetCore.Authorization;
using KronoxApi.Extensions;

namespace KronoxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireApiKey]
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
        var pages = await _context.CustomPages
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.DisplayName)
            .ToListAsync();
        
        var dtos = pages.Select(p => new CustomPageDto
        {
            Id = p.Id,
            PageKey = p.PageKey,
            Title = p.Title,
            DisplayName = p.DisplayName,
            Description = p.Description,
            IsActive = p.IsActive,
            ShowInNavigation = p.ShowInNavigation,
            NavigationType = p.NavigationType,
            ParentPageKey = p.ParentPageKey,
            SortOrder = p.SortOrder,
            CreatedAt = p.CreatedAt,
            LastModified = p.LastModified,
            CreatedBy = p.CreatedBy,
            RequiredRoles = p.RequiredRoles
        }).ToList();
        
        return Ok(dtos);
    }

    [HttpGet("navigation")]
    public async Task<ActionResult<List<NavigationPageDto>>> GetNavigationPages()
    {
        var pages = await _context.CustomPages
            .Where(p => p.IsActive && p.ShowInNavigation)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.DisplayName)
            .ToListAsync();
        
        // Gruppera sidor hierarkiskt
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

            // Lägg till barn-sidor (ALLA barn, inte bara de med tom ParentPageKey)
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
        
        // Även statiska sidor som kan ha dropdown-barn
        var staticPages = new List<string> { "omkonsortiet", "omsystemet", "visioner", "kontakt", "dokument", "forvaltning", "medlemsnytt" };
        
        foreach (var staticPageKey in staticPages)
        {
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
                
            if (children.Any())
            {
                navigationPages.Add(new NavigationPageDto
                {
                    PageKey = staticPageKey,
                    DisplayName = GetStaticPageDisplayName(staticPageKey),
                    NavigationType = "main",
                    ParentPageKey = null,
                    SortOrder = GetStaticPageSortOrder(staticPageKey),
                    RequiredRoles = new List<string>(),
                    Children = children
                });
            }
        }
        
        return Ok(navigationPages.OrderBy(p => p.SortOrder).ToList());
    }

    private string GetStaticPageDisplayName(string pageKey) => pageKey switch
    {
        "omkonsortiet" => "Om konsortiet",
        "omsystemet" => "Om systemet", 
        "visioner" => "Visioner & verksamhetsidé",
        "kontakt" => "Kontakta oss",
        "dokument" => "Dokument",
        "forvaltning" => "Förvaltning",
        "medlemsnytt" => "Medlemsnytt",
        _ => pageKey
    };

    private int GetStaticPageSortOrder(string pageKey) => pageKey switch
    {
        "omkonsortiet" => 1,
        "omsystemet" => 2,
        "visioner" => 3,
        "kontakt" => 17,
        "dokument" => 10,
        "forvaltning" => 11,
        "medlemsnytt" => 5,
        _ => 99
    };

    [HttpGet("{pageKey}")]
    [RequireRole("Admin")]
    public async Task<ActionResult<CustomPageDto>> GetCustomPage(string pageKey)
    {
        var page = await _context.CustomPages
            .FirstOrDefaultAsync(p => p.PageKey == pageKey);
        
        if (page == null)
        {
            return NotFound();
        }
        
        var dto = new CustomPageDto
        {
            Id = page.Id,
            PageKey = page.PageKey,
            Title = page.Title,
            DisplayName = page.DisplayName,
            Description = page.Description,
            IsActive = page.IsActive,
            ShowInNavigation = page.ShowInNavigation,
            NavigationType = page.NavigationType,
            ParentPageKey = page.ParentPageKey,
            SortOrder = page.SortOrder,
            CreatedAt = page.CreatedAt,
            LastModified = page.LastModified,
            CreatedBy = page.CreatedBy,
            RequiredRoles = page.RequiredRoles
        };
        
        return Ok(dto);
    }

    [HttpPost]
    [RequireRole("Admin")]
    public async Task<ActionResult<CustomPageDto>> CreateCustomPage(CreateCustomPageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

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

        // Skapa grundläggande ContentBlock för sidan
        var contentBlock = new ContentBlock
        {
            PageKey = request.PageKey,
            Title = request.Title,
            HtmlContent = "<p>Välkommen till denna nya sida. Redigera innehållet via adminpanelen.</p>",
            LastModified = DateTime.UtcNow
        };

        _context.ContentBlocks.Add(contentBlock);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Custom page created: {PageKey} by {User}", request.PageKey, User.Identity?.Name);

        // Skapa NavigationConfig ENDAST för huvudsidor (inte dropdown-barn)
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
                LastModified = DateTime.UtcNow
            };
            
            _context.NavigationConfigs.Add(navigationConfig);
            await _context.SaveChangesAsync();
        }
        
        return Ok(customPage.ToDto());
    }

    [HttpPut("{pageKey}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateCustomPage(string pageKey, UpdateCustomPageRequest request)
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
        page.ParentPageKey = request.ParentPageKey;
        page.SortOrder = request.SortOrder;
        page.RequiredRoles = request.RequiredRoles;
        page.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var existingNavConfig = await _context.NavigationConfigs
            .FirstOrDefaultAsync(n => n.PageKey == pageKey);

        if (request.ShowInNavigation && request.NavigationType == "main")
        {
            // Skapa/uppdatera NavigationConfig för huvudsidor
            if (existingNavConfig != null)
            {
                existingNavConfig.DisplayName = request.DisplayName;
                existingNavConfig.SortOrder = request.SortOrder;
                existingNavConfig.IsVisibleToGuests = !request.RequiredRoles.Any();
                existingNavConfig.IsActive = request.IsActive;
                existingNavConfig.LastModified = DateTime.UtcNow;
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
                    LastModified = DateTime.UtcNow
                };
                
                _context.NavigationConfigs.Add(navigationConfig);
            }
        }
        else
        {
            // Ta bort NavigationConfig för dropdown-sidor eller dolda sidor
            if (existingNavConfig != null)
            {
                _context.NavigationConfigs.Remove(existingNavConfig);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Custom page updated: {PageKey} by {User}", pageKey, User.Identity?.Name);

        return NoContent();
    }

    [HttpDelete("{pageKey}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> DeleteCustomPage(string pageKey)
    {
        var page = await _context.CustomPages
            .FirstOrDefaultAsync(p => p.PageKey == pageKey);
        
        if (page == null)
        {
            return NotFound();
        }

        // ✅ Ta bort NavigationConfig först
        var navigationConfig = await _context.NavigationConfigs
            .FirstOrDefaultAsync(n => n.PageKey == pageKey);
        if (navigationConfig != null)
        {
            _context.NavigationConfigs.Remove(navigationConfig);
        }

        // Ta bort relaterat innehåll
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

        _logger.LogInformation("Custom page deleted: {PageKey} by {User}", pageKey, User.Identity?.Name);

        return NoContent();
    }
}