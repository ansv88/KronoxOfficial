using KronoxApi.Attributes;
using KronoxApi.Data;
using KronoxApi.DTOs;
using KronoxApi.Extensions;
using KronoxApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<NavigationConfigDto>>> GetNavigation()
    {
        var configs = await _context.NavigationConfigs
            .OrderBy(n => n.SortOrder)
            .ToListAsync();
            
        return Ok(configs.ToDtos());
    }

    [HttpGet("page/{pageKey}")]
    [AllowAnonymous]
    public async Task<ActionResult<NavigationConfigDto>> GetByPageKey(string pageKey)
    {
        var config = await _context.NavigationConfigs
            .FirstOrDefaultAsync(n => n.PageKey == pageKey);
            
        if (config == null)
            return NotFound();
            
        return Ok(config.ToDto());
    }

    [HttpPost]
    [RequireRole("Admin")]
    [RequireApiKey]
    public async Task<ActionResult<NavigationConfigDto>> CreateNavigation([FromBody] object request)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(request);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            var config = new NavigationConfig
            {
                PageKey = root.GetProperty("pageKey").GetString() ?? "",
                DisplayName = root.GetProperty("displayName").GetString() ?? "",
                ItemType = root.GetProperty("itemType").GetString() ?? "",
                SortOrder = root.GetProperty("sortOrder").GetInt32(),
                IsVisibleToGuests = root.GetProperty("isVisibleToGuests").GetBoolean(),
                IsVisibleToMembers = root.GetProperty("isVisibleToMembers").GetBoolean(),
                IsActive = root.GetProperty("isActive").GetBoolean(),
                IsSystemItem = root.TryGetProperty("isSystemItem", out var sysItem) ? sysItem.GetBoolean() : false,
                RequiredRoles = root.TryGetProperty("requiredRoles", out var reqRoles) ? reqRoles.GetString() : null,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            
            _context.NavigationConfigs.Add(config);
            await _context.SaveChangesAsync();
            
            return Ok(config.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating navigation config");
            return BadRequest($"Invalid request format: {ex.Message}");
        }
    }

    [HttpPut("{id}")]
    [RequireRole("Admin")]
    [RequireApiKey]
    public async Task<IActionResult> UpdateNavigation(int id, [FromBody] NavigationUpdateDto request)
    {
        var config = await _context.NavigationConfigs.FindAsync(id);
        if (config == null) 
        {
            _logger.LogWarning("Navigation config not found: {Id}", id);
            return NotFound($"Navigation config with ID {id} not found");
        }

        try
        {
            _logger.LogInformation("Updating navigation config: {PageKey}, ID: {Id}, IsSystemItem: {IsSystemItem}", 
                config.PageKey, config.Id, config.IsSystemItem);

            // Kontrollera om displayName kan ändras
            var protectedItems = new[] { "admin", "logout" };
            var canEditDisplayName = !protectedItems.Contains(config.PageKey.ToLower());

            if (canEditDisplayName)
            {
                config.DisplayName = request.DisplayName;
                _logger.LogInformation("Updated DisplayName for {PageKey}: {DisplayName}", config.PageKey, request.DisplayName);
            }
            else
            {
                _logger.LogInformation("DisplayName not updated for protected item: {PageKey}", config.PageKey);
            }
            
            var fullyProtectedItems = new[] { "admin", "logout" };
            var isFullyProtected = fullyProtectedItems.Contains(config.PageKey.ToLower());
            
            if (!isFullyProtected)
            {
                config.IsVisibleToGuests = request.IsVisibleToGuests;
                config.IsVisibleToMembers = request.IsVisibleToMembers;
                _logger.LogInformation("Updated visibility for {PageKey}: Guests={IsVisibleToGuests}, Members={IsVisibleToMembers}", 
                    config.PageKey, request.IsVisibleToGuests, request.IsVisibleToMembers);
            }
            else
            {
                _logger.LogInformation("Visibility not updated for fully protected item: {PageKey}", config.PageKey);
            }
            
            // Uppdatera alla sortordrar
            config.SortOrder = request.SortOrder;
            config.GuestSortOrder = request.GuestSortOrder;
            config.MemberSortOrder = request.MemberSortOrder;
            config.IsActive = request.IsActive;
            config.LastModified = DateTime.UtcNow;

            // Uppdatera RequiredRoles
            config.RequiredRoles = request.RequiredRoles;

            _logger.LogInformation("About to save changes for {PageKey}", config.PageKey);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Navigation config updated successfully: {PageKey}, GuestSort: {GuestSort}, MemberSort: {MemberSort}", 
                config.PageKey, config.GuestSortOrder, config.MemberSortOrder);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating navigation config {Id} for PageKey {PageKey}. Request: {@Request}", 
                id, config.PageKey, request);
            return BadRequest($"Error updating navigation for {config.PageKey}: {ex.Message}");
        }
    }

    [HttpPut("reorder")]
    [RequireRole("Admin")]
    [RequireApiKey]
    public async Task<IActionResult> ReorderNavigation([FromBody] List<object> items)
    {
        try
        {
            foreach (var item in items)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(item);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                var id = root.GetProperty("id").GetInt32();
                var sortOrder = root.GetProperty("sortOrder").GetInt32();
                
                var config = await _context.NavigationConfigs.FindAsync(id);
                if (config != null)
                {
                    config.SortOrder = sortOrder;
                    config.LastModified = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering navigation");
            return BadRequest($"Error reordering navigation: {ex.Message}");
        }
    }

    // Auto-registrera nya CustomPages
    [HttpPost("register-custom")]
    [RequireRole("Admin")]
    [RequireApiKey]
    public async Task<IActionResult> RegisterCustomPage([FromBody] object request)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(request);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            var pageKey = root.GetProperty("pageKey").GetString() ?? "";
            
            var existingConfig = await _context.NavigationConfigs
                .FirstOrDefaultAsync(n => n.PageKey == pageKey);

            if (existingConfig == null)
            {
                var config = new NavigationConfig
                {
                    PageKey = pageKey,
                    DisplayName = root.GetProperty("displayName").GetString() ?? "",
                    ItemType = "custom",
                    SortOrder = root.GetProperty("sortOrder").GetInt32(),
                    IsVisibleToGuests = root.GetProperty("isVisibleToGuests").GetBoolean(),
                    IsVisibleToMembers = root.GetProperty("isVisibleToMembers").GetBoolean(),
                    IsActive = true,
                    RequiredRoles = root.TryGetProperty("requiredRoles", out var reqRoles) ? reqRoles.GetString() : null,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                _context.NavigationConfigs.Add(config);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering custom page");
            return BadRequest($"Error registering custom page: {ex.Message}");
        }
    }
}