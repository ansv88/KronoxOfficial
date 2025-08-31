using KronoxApi.Attributes;
using KronoxApi.Data;
using KronoxApi.DTOs;
using KronoxApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireApiKey]
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
                PlannedDelivery = i.PlannedDelivery,
                Completed = i.Completed,
                SortOrder = i.SortOrder
            }).ToList()
        };
    }

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

            // Lägg till nya items
            actionPlan.Items = dto.Items.Select(i => new ActionPlanItem
            {
                Priority = i.Priority,
                Module = i.Module,
                Activity = i.Activity,
                PlannedDelivery = i.PlannedDelivery,
                Completed = i.Completed,
                SortOrder = i.SortOrder
            }).ToList();

            actionPlan.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av handlingsplan för {PageKey}", pageKey);
            return StatusCode(500, "Ett fel uppstod vid uppdatering av handlingsplanen");
        }
    }
}