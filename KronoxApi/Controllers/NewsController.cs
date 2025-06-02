using KronoxApi.Data;
using KronoxApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Alla endpoints i den här controllern kräver att användaren är inloggad
public class NewsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<NewsController> _logger;

    public NewsController(ApplicationDbContext dbContext, ILogger<NewsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }


    // Skapar ett nytt nyhetsinlägg i systemet. Inlägget får automatiskt aktuellt datum som publiceringsdatum.
    [HttpPost("create")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateNewsPost([FromBody] NewsModel post)
    {
        try
        {
            post.PublishedDate = DateTime.UtcNow;
            _dbContext.NewsModel.Add(post);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Nytt nyhetsinlägg skapat med ID {Id}", post.Id);
            return Ok(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid skapandet av ett nyhetsinlägg");
            return StatusCode(500, "Ett oväntat fel inträffade vid skapandet av nyhetsinlägget");
        }
    }


    // Uppdaterar ett befintligt nyhetsinlägg baserat på ID. Rubrik, innehåll och arkiveringsstatus kan ändras.
    [HttpPut("update/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateNewsPost(int id, [FromBody] NewsModel updatedPost)
    {
        try
        {
            var post = await _dbContext.NewsModel.FindAsync(id);
            if (post == null)
            {
                _logger.LogWarning("Försök att uppdatera nyhetsinlägg som inte finns, ID: {Id}", id);
                return NotFound("Nyhetsinlägget hittades inte");
            }

            post.Title = updatedPost.Title;
            post.Content = updatedPost.Content;
            post.IsArchived = updatedPost.IsArchived;

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Nyhetsinlägg med ID {Id} uppdaterat", id);
            return Ok(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid uppdatering av nyhetsinlägg med ID {Id}", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid uppdatering av nyhetsinlägget");
        }
    }


    // Tar bort ett nyhetsinlägg permanent från databasen.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteNewsPost(int id)
    {
        try
        {
            var post = await _dbContext.NewsModel.FindAsync(id);
            if (post == null)
            {
                _logger.LogWarning("Försök att radera nyhetsinlägg som inte finns, ID: {Id}", id);
                return NotFound("Nyhetsinlägget hittades inte");
            }

            _dbContext.NewsModel.Remove(post);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Nyhetsinlägg med ID {Id} har raderats", id);
            return Ok("Nyhetsinlägget har tagits bort");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid borttagning av nyhetsinlägg med ID {Id}", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid borttagning av nyhetsinlägget");
        }
    }


    // Hämtar nyhetsinlägg med möjlighet att filtrera på arkiverade inlägg och datumintervall. Resultatet sorteras med nyaste inlägg först. (alla inloggade)
    [HttpGet("filtered")]
    public async Task<IActionResult> GetFilteredNews([FromQuery] bool? archived, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var query = _dbContext.NewsModel.AsQueryable();

            if (archived.HasValue)
            {
                query = query.Where(n => n.IsArchived == archived.Value);
            }
            if (from.HasValue)
            {
                query = query.Where(n => n.PublishedDate >= from.Value);
            }
            if (to.HasValue)
            {
                query = query.Where(n => n.PublishedDate <= to.Value);
            }

            query = query.OrderByDescending(n => n.PublishedDate);

            var posts = await query.ToListAsync();
            _logger.LogInformation("Filtrerad nyhetslista hämtad med {Count} träffar", posts.Count);
            return Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid hämtning av filtrerade nyheter");
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av filtrerade nyheter");
        }
    }


    // Hämtar ett specifikt nyhetsinlägg baserat på ID (alla inloggade)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetNewsPost(int id)
    {
        try
        {
            var post = await _dbContext.NewsModel.FindAsync(id);
            if (post == null)
            {
                return NotFound("Nyhetsinlägget hittades inte.");
            }
            return Ok(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid hämtning av nyhetsinlägg med ID {Id}.", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av nyhetsinlägget.");
        }
    }


    // Hämtar alla nyhetsinlägg i systemet utan filtrering (alla inloggade)
    [HttpGet("all")]
    public async Task<IActionResult> GetAllNewsPosts()
    {
        try
        {
            var posts = await _dbContext.NewsModel.ToListAsync();
            return Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid hämtning av alla nyhetsinlägg.");
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av alla nyhetsinlägg.");
        }
    }
}