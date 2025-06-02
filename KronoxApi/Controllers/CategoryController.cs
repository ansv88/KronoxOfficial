using KronoxApi.Attributes;
using KronoxApi.Data;
using KronoxApi.DTOs;
using KronoxApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Controllers;

/// <summary>
/// API-kontroller för hantering av huvud- och underkategorier.
/// Endast åtkomlig för administratörer.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[RequireApiKey]
public class CategoryController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CategoryController> _log;

    public CategoryController(ApplicationDbContext dbContext, ILogger<CategoryController> log)
    {
        _dbContext = dbContext;
        _log = log;
    }


    // Hämtar alla huvudkategorier.
    [HttpGet("main")]
    [RequireRole("Admin")]
    public async Task<IActionResult> ListAllMainCategories()
    {
        var allMainCategories = await _dbContext.MainCategories.ToListAsync();
        return Ok(allMainCategories);
    }


    // Hämtar alla underkategorier.
    [HttpGet("sub")]
    [RequireRole("Admin")]
    public async Task<IActionResult> ListAllSubCategories()
    {
        var allSubCategories = await _dbContext.SubCategories.ToListAsync();
        return Ok(allSubCategories);
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
            return Ok(mainCategory);
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
            return Ok(subCategory);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ett fel inträffade vid hämtning av underkategorin med ID {Id}.", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av kategorin.");
        }
    }


    // Skapar en ny huvudkategori.
    [HttpPost("main")]
    [RequireRole("Admin")]
    public async Task<IActionResult> CreateMainCategory([FromBody] MainCategory model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            _log.LogWarning("Försök att skapa huvudkategori utan namn.");
            return BadRequest("Namn krävs.");
        }

        try
        {
            _dbContext.MainCategories.Add(model);
            await _dbContext.SaveChangesAsync();

            var mainCategoryDto = new MainCategoryDto
            {
                Id = model.Id,
                Name = model.Name
            };
            return Ok(mainCategoryDto);
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
    public async Task<IActionResult> CreateSubCategory([FromBody] SubCategory model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            _log.LogWarning("Försök att skapa underkategori utan namn.");
            return BadRequest("Namn krävs.");
        }

        try
        {
            _dbContext.SubCategories.Add(model);
            await _dbContext.SaveChangesAsync();

            var subCategoryDto = new SubCategoryDto
            {
                Id = model.Id,
                Name = model.Name
            };
            return Ok(subCategoryDto);
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
    public async Task<IActionResult> UpdateMainCategory(int id, [FromBody] MainCategory update)
    {
        try
        {
            var mainCategory = await _dbContext.MainCategories.FindAsync(id);
            if (mainCategory == null)
            {
                _log.LogWarning("Huvudkategorin med ID {Id} hittades inte för uppdatering.", id);
                return NotFound("Kategorin hittades inte.");
            }

            if (string.IsNullOrWhiteSpace(update.Name))
            {
                _log.LogWarning("Försök att uppdatera huvudkategori med tomt namn.");
                return BadRequest("Namn krävs.");
            }

            mainCategory.Name = update.Name;
            await _dbContext.SaveChangesAsync();
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
    public async Task<IActionResult> UpdateSubCategory(int id, [FromBody] SubCategory update)
    {
        try
        {
            var subCategory = await _dbContext.SubCategories.FindAsync(id);
            if (subCategory == null)
            {
                _log.LogWarning("Underkategorin med ID {Id} hittades inte för uppdatering.", id);
                return NotFound("Underkategorin hittades inte.");
            }

            if (string.IsNullOrWhiteSpace(update.Name))
            {
                _log.LogWarning("Försök att uppdatera underkategori med tomt namn.");
                return BadRequest("Namn krävs.");
            }

            subCategory.Name = update.Name;
            await _dbContext.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ett fel inträffade vid uppdatering av underkategorin med ID {Id}.", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid uppdatering av underkategorin.");
        }
    }


    // Tar bort en huvudkategori.
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

            _dbContext.MainCategories.Remove(mainCategory);
            await _dbContext.SaveChangesAsync();
            return Ok($"Huvudkategori '{mainCategory.Name}' har tagits bort.");
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

            _dbContext.SubCategories.Remove(subCategory);
            await _dbContext.SaveChangesAsync();

            return Ok($"Underkategori '{subCategory.Name}' har tagits bort.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ett fel inträffade vid borttagning av underkategorin med ID {Id}.", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid borttagning av underkategorin.");
        }
    }
}