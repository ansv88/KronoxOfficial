using KronoxApi.Attributes;
using KronoxApi.Data;
using KronoxApi.DTOs;
using KronoxApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Controllers;

/// <summary>
/// API‑kontroller för handlingsplaner per sida (pageKey).
/// Ger Admin CRUD på åtgärdsposter samt omordning; uppdaterar LastModified.
/// Skyddad med API‑nyckel och __EnableRateLimiting("API")__.
/// </summary>

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
    public async Task<ActionResult<ActionPlanTableDto>> GetActionPlan(string pageKey, [FromQuery] bool includeArchived = false)
    {
        var actionPlan = await _context.ActionPlanTables
            .Include(t => t.Items.OrderBy(i => i.SortOrder))
                .ThenInclude(i => i.Subgoals.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync(t => t.PageKey == pageKey);

        if (actionPlan == null)
        {
            return new ActionPlanTableDto
            {
                PageKey = pageKey,
                Items = new List<ActionPlanItemDto>()
            };
        }

        // Arkiverade poster returneras nar admin begar dem (includeArchived) eller
        // nar admin valt att visa dem publikt (ShowArchivedPublicly).
        var includeArchivedItems = includeArchived || actionPlan.ShowArchivedPublicly;

        return new ActionPlanTableDto
        {
            Id = actionPlan.Id,
            PageKey = actionPlan.PageKey,
            LastModified = actionPlan.LastModified,
            ShowArchivedPublicly = actionPlan.ShowArchivedPublicly,
            Items = actionPlan.Items
                .Where(i => !i.IsArchived)
                .OrderBy(i => i.SortOrder)
                .Select(MapItem)
                .ToList(),
            ArchivedItems = includeArchivedItems
                ? actionPlan.Items
                    .Where(i => i.IsArchived)
                    .OrderByDescending(i => i.ArchivedAt)
                    .Select(MapItem)
                    .ToList()
                : new List<ActionPlanItemDto>()
        };
    }

    private static ActionPlanItemDto MapItem(ActionPlanItem i) => new()
    {
        Id = i.Id,
        Module = i.Module,
        Activity = i.Activity,
        DetailedDescription = i.DetailedDescription,
        PlannedDelivery = i.PlannedDelivery,
        Completed = i.Completed,
        SortOrder = i.SortOrder,
        IsArchived = i.IsArchived,
        ArchivedAt = i.ArchivedAt,
        Subgoals = i.Subgoals.OrderBy(s => s.SortOrder).Select(MapSubgoal).ToList()
    };

    private static ActionPlanSubgoalDto MapSubgoal(ActionPlanSubgoal s) => new()
    {
        Id = s.Id,
        Activity = s.Activity,
        DetailedDescription = s.DetailedDescription,
        PlannedDelivery = s.PlannedDelivery,
        Completed = s.Completed,
        SortOrder = s.SortOrder
    };

    [HttpPost("{pageKey}/items")]
    [RequireRole("Admin")]
    public async Task<ActionResult<ActionPlanItemDto>> CreateActionPlanItem(string pageKey, [FromBody] CreateActionPlanItemDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var actionPlan = await _context.ActionPlanTables
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.PageKey == pageKey);

            if (actionPlan == null)
            {
                actionPlan = new ActionPlanTable { PageKey = pageKey };
                _context.ActionPlanTables.Add(actionPlan);
                await _context.SaveChangesAsync();
            }

            var newItem = new ActionPlanItem
            {
                ActionPlanTableId = actionPlan.Id,
                Module = dto.Module,
                Activity = dto.Activity,
                DetailedDescription = dto.DetailedDescription,
                PlannedDelivery = dto.PlannedDelivery,
                Completed = dto.Completed,
                SortOrder = actionPlan.Items.Count(i => !i.IsArchived)
            };

            _context.ActionPlanItems.Add(newItem);
            actionPlan.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var result = MapItem(newItem);

            _logger.LogDebug("Skapade nytt handlingsplan-item {Id} för {PageKey}", newItem.Id, pageKey);
            return CreatedAtAction(nameof(GetActionPlan), new { pageKey }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid skapande av handlingsplan-item för {PageKey}", pageKey);
            return StatusCode(500, "Ett fel uppstod vid skapande av åtgärden");
        }
    }

    [HttpPut("{pageKey}/items/{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateActionPlanItem(string pageKey, int id, [FromBody] CreateActionPlanItemDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var item = await _context.ActionPlanItems
                .Include(i => i.ActionPlanTable)
                .FirstOrDefaultAsync(i => i.Id == id && i.ActionPlanTable.PageKey == pageKey);

            if (item == null)
            {
                return NotFound();
            }

            item.Module = dto.Module;
            item.Activity = dto.Activity;
            item.DetailedDescription = dto.DetailedDescription;
            item.PlannedDelivery = dto.PlannedDelivery;
            item.Completed = dto.Completed;

            item.ActionPlanTable.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogDebug("Uppdaterade handlingsplan-item {Id} för {PageKey}", id, pageKey);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av handlingsplan-item {Id} för {PageKey}", id, pageKey);
            return StatusCode(500, "Ett fel uppstod vid uppdatering av åtgärden");
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
            var wasArchived = item.IsArchived;
            var actionPlan = item.ActionPlanTable;

            _context.ActionPlanItems.Remove(item);

            // Omindexera endast aktiva poster så numreringen förblir sammanhängande.
            if (!wasArchived)
            {
                var remainingItems = actionPlan.Items
                    .Where(i => i.Id != id && !i.IsArchived && i.SortOrder > sortOrder)
                    .OrderBy(i => i.SortOrder);

                foreach (var remainingItem in remainingItems)
                {
                    remainingItem.SortOrder--;
                }
            }

            actionPlan.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogDebug("Tog bort handlingsplan-item {Id} från {PageKey}", id, pageKey);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av handlingsplan-item {Id} från {PageKey}", id, pageKey);
            return StatusCode(500, "Ett fel uppstod vid borttagning av åtgärden");
        }
    }

    // ---------- Arkivering ----------

    [HttpPost("{pageKey}/items/{id}/archive")]
    [RequireRole("Admin")]
    public async Task<IActionResult> ArchiveActionPlanItem(string pageKey, int id)
    {
        try
        {
            var item = await _context.ActionPlanItems
                .Include(i => i.ActionPlanTable)
                .ThenInclude(t => t.Items)
                .FirstOrDefaultAsync(i => i.Id == id && i.ActionPlanTable.PageKey == pageKey);

            if (item == null) return NotFound();
            if (item.IsArchived) return NoContent();

            var sortOrder = item.SortOrder;
            var actionPlan = item.ActionPlanTable;

            item.IsArchived = true;
            item.ArchivedAt = DateTime.UtcNow;
            item.SortOrder = 0;

            // Omindexera kvarvarande aktiva poster så numreringen förblir sammanhängande.
            foreach (var active in actionPlan.Items
                .Where(i => i.Id != id && !i.IsArchived && i.SortOrder > sortOrder)
                .OrderBy(i => i.SortOrder))
            {
                active.SortOrder--;
            }

            actionPlan.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Arkiverade handlingsplan-item {Id} i {PageKey}", id, pageKey);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid arkivering av handlingsplan-item {Id} i {PageKey}", id, pageKey);
            return StatusCode(500, "Ett fel uppstod vid arkivering av åtgärden");
        }
    }

    [HttpPost("{pageKey}/items/{id}/unarchive")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UnarchiveActionPlanItem(string pageKey, int id)
    {
        try
        {
            var item = await _context.ActionPlanItems
                .Include(i => i.ActionPlanTable)
                .ThenInclude(t => t.Items)
                .FirstOrDefaultAsync(i => i.Id == id && i.ActionPlanTable.PageKey == pageKey);

            if (item == null) return NotFound();
            if (!item.IsArchived) return NoContent();

            var actionPlan = item.ActionPlanTable;

            item.IsArchived = false;
            item.ArchivedAt = null;
            item.SortOrder = actionPlan.Items.Count(i => !i.IsArchived && i.Id != id);

            actionPlan.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Återställde handlingsplan-item {Id} i {PageKey}", id, pageKey);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid återställning av handlingsplan-item {Id} i {PageKey}", id, pageKey);
            return StatusCode(500, "Ett fel uppstod vid återställning av åtgärden");
        }
    }

    [HttpPut("{pageKey}/show-archived")]
    [RequireRole("Admin")]
    public async Task<IActionResult> SetShowArchived(string pageKey, [FromBody] SetShowArchivedDto dto)
    {
        if (dto == null) return BadRequest("Ogiltig begäran.");

        try
        {
            var actionPlan = await _context.ActionPlanTables
                .FirstOrDefaultAsync(t => t.PageKey == pageKey);

            if (actionPlan == null)
            {
                actionPlan = new ActionPlanTable { PageKey = pageKey };
                _context.ActionPlanTables.Add(actionPlan);
            }

            actionPlan.ShowArchivedPublicly = dto.ShowArchivedPublicly;
            actionPlan.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Satte ShowArchivedPublicly={Value} för {PageKey}", dto.ShowArchivedPublicly, pageKey);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av arkivsynlighet för {PageKey}", pageKey);
            return StatusCode(500, "Ett fel uppstod vid uppdatering av arkivsynligheten");
        }
    }

    [HttpPost("{pageKey}/items/{id}/move")]
    [RequireRole("Admin")]
    public async Task<IActionResult> MoveActionPlanItem(string pageKey, int id, [FromBody] MoveItemRequest request)
    {
        if (request == null) return BadRequest("Ogiltig begäran.");

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
                return NotFound("Åtgärd hittades inte");
            }

            if (item.IsArchived)
            {
                return BadRequest("Arkiverade poster kan inte flyttas");
            }

            var sortedItems = actionPlan.Items.Where(i => !i.IsArchived).OrderBy(i => i.SortOrder).ToList();
            var currentIndex = sortedItems.FindIndex(i => i.Id == id);
            var newIndex = currentIndex + request.Direction;

            if (newIndex < 0 || newIndex >= sortedItems.Count)
            {
                return BadRequest("Ogiltig flyttning");
            }

            sortedItems.RemoveAt(currentIndex);
            sortedItems.Insert(newIndex, item);

            for (int i = 0; i < sortedItems.Count; i++)
            {
                sortedItems[i].SortOrder = i;
            }

            actionPlan.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Flyttade handlingsplan-item {Id} i {PageKey}", id, pageKey);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid flyttning av handlingsplan-item {Id} i {PageKey}", id, pageKey);
            return StatusCode(500, "Ett fel uppstod vid flyttning av åtgärden");
        }
    }

    // ---------- Delmål (subgoals) ----------

    [HttpPost("{pageKey}/items/{itemId}/subgoals")]
    [RequireRole("Admin")]
    public async Task<ActionResult<ActionPlanSubgoalDto>> CreateSubgoal(string pageKey, int itemId, [FromBody] CreateActionPlanSubgoalDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var item = await _context.ActionPlanItems
                .Include(i => i.ActionPlanTable)
                .Include(i => i.Subgoals)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.ActionPlanTable.PageKey == pageKey);

            if (item == null) return NotFound("Åtgärd hittades inte");

            var subgoal = new ActionPlanSubgoal
            {
                ActionPlanItemId = item.Id,
                Activity = dto.Activity,
                DetailedDescription = dto.DetailedDescription,
                PlannedDelivery = dto.PlannedDelivery,
                Completed = dto.Completed,
                SortOrder = item.Subgoals.Count
            };

            _context.ActionPlanSubgoals.Add(subgoal);
            item.ActionPlanTable.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var result = MapSubgoal(subgoal);

            _logger.LogDebug("Skapade delmål {Id} för åtgärd {ItemId} i {PageKey}", subgoal.Id, itemId, pageKey);
            return CreatedAtAction(nameof(GetActionPlan), new { pageKey }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid skapande av delmål för åtgärd {ItemId} i {PageKey}", itemId, pageKey);
            return StatusCode(500, "Ett fel uppstod vid skapande av delmålet");
        }
    }

    [HttpPut("{pageKey}/items/{itemId}/subgoals/{subgoalId}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateSubgoal(string pageKey, int itemId, int subgoalId, [FromBody] CreateActionPlanSubgoalDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var subgoal = await _context.ActionPlanSubgoals
                .Include(s => s.ActionPlanItem)
                .ThenInclude(i => i.ActionPlanTable)
                .FirstOrDefaultAsync(s => s.Id == subgoalId
                    && s.ActionPlanItemId == itemId
                    && s.ActionPlanItem.ActionPlanTable.PageKey == pageKey);

            if (subgoal == null) return NotFound();

            subgoal.Activity = dto.Activity;
            subgoal.DetailedDescription = dto.DetailedDescription;
            subgoal.PlannedDelivery = dto.PlannedDelivery;
            subgoal.Completed = dto.Completed;
            subgoal.ActionPlanItem.ActionPlanTable.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogDebug("Uppdaterade delmål {Id} i {PageKey}", subgoalId, pageKey);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av delmål {Id} i {PageKey}", subgoalId, pageKey);
            return StatusCode(500, "Ett fel uppstod vid uppdatering av delmålet");
        }
    }

    [HttpDelete("{pageKey}/items/{itemId}/subgoals/{subgoalId}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> DeleteSubgoal(string pageKey, int itemId, int subgoalId)
    {
        try
        {
            var subgoal = await _context.ActionPlanSubgoals
                .Include(s => s.ActionPlanItem)
                .ThenInclude(i => i.ActionPlanTable)
                .Include(s => s.ActionPlanItem)
                .ThenInclude(i => i.Subgoals)
                .FirstOrDefaultAsync(s => s.Id == subgoalId
                    && s.ActionPlanItemId == itemId
                    && s.ActionPlanItem.ActionPlanTable.PageKey == pageKey);

            if (subgoal == null) return NotFound();

            var sortOrder = subgoal.SortOrder;
            var item = subgoal.ActionPlanItem;

            _context.ActionPlanSubgoals.Remove(subgoal);

            // Omindexera kvarvarande delmål så ordningen blir sammanhängande.
            foreach (var s in item.Subgoals.Where(s => s.Id != subgoalId && s.SortOrder > sortOrder).OrderBy(s => s.SortOrder))
            {
                s.SortOrder--;
            }

            item.ActionPlanTable.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Tog bort delmål {Id} från åtgärd {ItemId} i {PageKey}", subgoalId, itemId, pageKey);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av delmål {Id} i {PageKey}", subgoalId, pageKey);
            return StatusCode(500, "Ett fel uppstod vid borttagning av delmålet");
        }
    }

    [HttpPost("{pageKey}/items/{itemId}/subgoals/{subgoalId}/move")]
    [RequireRole("Admin")]
    public async Task<IActionResult> MoveSubgoal(string pageKey, int itemId, int subgoalId, [FromBody] MoveItemRequest request)
    {
        if (request == null) return BadRequest("Ogiltig begäran.");

        try
        {
            var item = await _context.ActionPlanItems
                .Include(i => i.ActionPlanTable)
                .Include(i => i.Subgoals)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.ActionPlanTable.PageKey == pageKey);

            if (item == null) return NotFound("Åtgärd hittades inte");

            var subgoal = item.Subgoals.FirstOrDefault(s => s.Id == subgoalId);
            if (subgoal == null) return NotFound("Delmål hittades inte");

            var sorted = item.Subgoals.OrderBy(s => s.SortOrder).ToList();
            var currentIndex = sorted.FindIndex(s => s.Id == subgoalId);
            var newIndex = currentIndex + request.Direction;

            if (newIndex < 0 || newIndex >= sorted.Count) return BadRequest("Ogiltig flyttning");

            sorted.RemoveAt(currentIndex);
            sorted.Insert(newIndex, subgoal);
            for (int i = 0; i < sorted.Count; i++) sorted[i].SortOrder = i;

            item.ActionPlanTable.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Flyttade delmål {Id} i åtgärd {ItemId} ({PageKey})", subgoalId, itemId, pageKey);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid flyttning av delmål {Id} i {PageKey}", subgoalId, pageKey);
            return StatusCode(500, "Ett fel uppstod vid flyttning av delmålet");
        }
    }
}

public class MoveItemRequest
{
    public int Direction { get; set; } // -1 eller 1
}