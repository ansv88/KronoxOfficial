using KronoxApi.Attributes;
using KronoxApi.Data;
using KronoxApi.DTOs;
using KronoxApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireApiKey]
[EnableRateLimiting("API")]
public class ActionPlanController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActionPlanController> _logger;

    public ActionPlanController(ApplicationDbContext context, ILogger<ActionPlanController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("{pageKey}")]
    public async Task<ActionResult<ActionPlanTableDto>> GetActionPlan(string pageKey)
    {
        var actionPlan = await _context.ActionPlanTables
            .Include(t => t.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(t => t.PageKey == pageKey);

        if (actionPlan == null)
        {
            return new ActionPlanTableDto 
            { 
                PageKey = pageKey, 
                Items = new List<ActionPlanItemDto>() 
            };
        }

        return new ActionPlanTableDto
        {
            Id = actionPlan.Id,
            PageKey = actionPlan.PageKey,
            LastModified = actionPlan.LastModified,
            Items = actionPlan.Items.Select(i => new ActionPlanItemDto
            {
                Id = i.Id,
                Priority = i.Priority,
                Module = i.Module,
                Activity = i.Activity,
                DetailedDescription = i.DetailedDescription,
                PlannedDelivery = i.PlannedDelivery,
                Completed = i.Completed,
                SortOrder = i.SortOrder
            }).ToList()
        };
    }

    [HttpPost("{pageKey}/items")]
    [RequireRole("Admin")]
    public async Task<ActionResult<ActionPlanItemDto>> CreateActionPlanItem(string pageKey, CreateActionPlanItemDto dto)
    {
        try
        {
            // Hitta eller skapa ActionPlanTable
            var actionPlan = await _context.ActionPlanTables
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.PageKey == pageKey);

            if (actionPlan == null)
            {
                actionPlan = new ActionPlanTable { PageKey = pageKey };
                _context.ActionPlanTables.Add(actionPlan);
                await _context.SaveChangesAsync(); // Spara f�r att f� ID
            }

            // Skapa nytt item
            var newItem = new ActionPlanItem
            {
                ActionPlanTableId = actionPlan.Id,
                Priority = dto.Priority,
                Module = dto.Module,
                Activity = dto.Activity,
                DetailedDescription = dto.DetailedDescription,
                PlannedDelivery = dto.PlannedDelivery,
                Completed = dto.Completed,
                SortOrder = actionPlan.Items.Count
            };

            _context.ActionPlanItems.Add(newItem);
            actionPlan.LastModified = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            var result = new ActionPlanItemDto
            {
                Id = newItem.Id,
                Priority = newItem.Priority,
                Module = newItem.Module,
                Activity = newItem.Activity,
                DetailedDescription = newItem.DetailedDescription,
                PlannedDelivery = newItem.PlannedDelivery,
                Completed = newItem.Completed,
                SortOrder = newItem.SortOrder
            };

            _logger.LogInformation("Skapade nytt handlingsplan-item {Id} f�r {PageKey}", newItem.Id, pageKey);
            return CreatedAtAction(nameof(GetActionPlan), new { pageKey }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid skapande av handlingsplan-item f�r {PageKey}", pageKey);
            return StatusCode(500, "Ett fel uppstod vid skapande av �tg�rden");
        }
    }

    [HttpPut("{pageKey}/items/{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateActionPlanItem(string pageKey, int id, CreateActionPlanItemDto dto)
    {
        try
        {
            var item = await _context.ActionPlanItems
                .Include(i => i.ActionPlanTable)
                .FirstOrDefaultAsync(i => i.Id == id && i.ActionPlanTable.PageKey == pageKey);

            if (item == null)
            {
                return NotFound();
            }

            item.Priority = dto.Priority;
            item.Module = dto.Module;
            item.Activity = dto.Activity;
            item.DetailedDescription = dto.DetailedDescription;
            item.PlannedDelivery = dto.PlannedDelivery;
            item.Completed = dto.Completed;
            
            item.ActionPlanTable.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Uppdaterade handlingsplan-item {Id} f�r {PageKey}", id, pageKey);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av handlingsplan-item {Id} f�r {PageKey}", id, pageKey);
            return StatusCode(500, "Ett fel uppstod vid uppdatering av �tg�rden");
        }
    }

    [HttpDelete("{pageKey}/items/{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> DeleteActionPlanItem(string pageKey, int id)
    {
        try
        {
            var item = await _context.ActionPlanItems
                .Include(i => i.ActionPlanTable)
                .ThenInclude(t => t.Items)
                .FirstOrDefaultAsync(i => i.Id == id && i.ActionPlanTable.PageKey == pageKey);

            if (item == null)
            {
                return NotFound();
            }

            var sortOrder = item.SortOrder;
            var actionPlan = item.ActionPlanTable;

            // Ta bort item
            _context.ActionPlanItems.Remove(item);

            // Uppdatera SortOrder f�r �terst�ende items
            var remainingItems = actionPlan.Items
                .Where(i => i.Id != id && i.SortOrder > sortOrder)
                .OrderBy(i => i.SortOrder);

            foreach (var remainingItem in remainingItems)
            {
                remainingItem.SortOrder--;
            }

            // Uppdatera LastModified
            actionPlan.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tog bort handlingsplan-item {Id} fr�n {PageKey}", id, pageKey);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av handlingsplan-item {Id} fr�n {PageKey}", id, pageKey);
            return StatusCode(500, "Ett fel uppstod vid borttagning av �tg�rden");
        }
    }

    // Beh�ll den gamla PUT-metoden f�r bak�tkompatibilitet
    [HttpPut("{pageKey}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateActionPlan(string pageKey, ActionPlanTableDto dto)
    {
        try
        {
            var actionPlan = await _context.ActionPlanTables
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.PageKey == pageKey);

            if (actionPlan == null)
            {
                actionPlan = new ActionPlanTable { PageKey = pageKey };
                _context.ActionPlanTables.Add(actionPlan);
            }

            // Ta bort befintliga items
            _context.ActionPlanItems.RemoveRange(actionPlan.Items);

            // L�gg till nya items
            actionPlan.Items = dto.Items.Select(i => new ActionPlanItem
            {
                Priority = i.Priority,
                Module = i.Module,
                Activity = i.Activity,
                DetailedDescription = i.DetailedDescription,
                PlannedDelivery = i.PlannedDelivery,
                Completed = i.Completed,
                SortOrder = i.SortOrder
            }).ToList();

            actionPlan.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Uppdaterade hela handlingsplanen f�r {PageKey}", pageKey);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av handlingsplan f�r {PageKey}", pageKey);
            return StatusCode(500, "Ett fel uppstod vid uppdatering av handlingsplanen");
        }
    }

    [HttpPost("{pageKey}/items/{id}/move")]
    [RequireRole("Admin")]
    public async Task<IActionResult> MoveActionPlanItem(string pageKey, int id, [FromBody] MoveItemRequest request)
    {
        try
        {
            var actionPlan = await _context.ActionPlanTables
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.PageKey == pageKey);

            if (actionPlan == null)
            {
                return NotFound("Handlingsplan hittades inte");
            }

            var item = actionPlan.Items.FirstOrDefault(i => i.Id == id);
            if (item == null)
            {
                return NotFound("�tg�rd hittades inte");
            }

            var sortedItems = actionPlan.Items.OrderBy(i => i.SortOrder).ToList();
            var currentIndex = sortedItems.FindIndex(i => i.Id == id);
            var newIndex = currentIndex + request.Direction;

            if (newIndex < 0 || newIndex >= sortedItems.Count)
            {
                return BadRequest("Ogiltig flyttning");
            }

            // Flytta item
            sortedItems.RemoveAt(currentIndex);
            sortedItems.Insert(newIndex, item);

            // Uppdatera SortOrder f�r alla items
            for (int i = 0; i < sortedItems.Count; i++)
            {
                sortedItems[i].SortOrder = i;
            }

            actionPlan.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Flyttade handlingsplan-item {Id} i {PageKey}", id, pageKey);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid flyttning av handlingsplan-item {Id} i {PageKey}", id, pageKey);
            return StatusCode(500, "Ett fel uppstod vid flyttning av �tg�rden");
        }
    }
}

// DTO f�r flyttning av items
public class MoveItemRequest
{
    public int Direction { get; set; } // -1 f�r upp, 1 f�r ner
}