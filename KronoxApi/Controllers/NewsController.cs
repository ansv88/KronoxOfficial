using KronoxApi.Attributes;
using KronoxApi.Data;
using KronoxApi.Models;
using KronoxApi.Requests;
using KronoxApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Controllers;

/// <summary>
/// API‑kontroller för nyheter (news): Admin‑CRUD, arkivering/avarkivering,
/// rollbaserad listning för medlemmar och åtkomstkontroll för enskilda nyheter.
/// Stöd för att koppla bort/ta bort dokument från nyheter. Skyddad med API‑nyckel.
/// </summary>

[ApiController]
[Route("api/[controller]")]
[RequireApiKey] // Kräver API-nyckel för alla endpoints
public class NewsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<NewsController> _logger;
    private readonly IRoleValidationService _roleValidationService;
    private const string RoleHeader = "X-User-Roles";

    public NewsController(
        ApplicationDbContext dbContext,
        ILogger<NewsController> logger,
        IRoleValidationService roleValidationService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _roleValidationService = roleValidationService;
    }

    // Skapar ett nytt nyhetsinlägg i systemet
    [HttpPost("create")]
    [RequireRole("Admin")]
    public async Task<IActionResult> CreateNewsPost([FromBody] CreateNewsRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            // Konvertera från lokal tid till UTC för lagring
            var publishDate = request.ScheduledPublishDate?.ToUniversalTime() ?? DateTime.UtcNow;

            var post = new NewsModel
            {
                Title = request.Title,
                Content = request.Content,
                ScheduledPublishDate = request.ScheduledPublishDate?.ToUniversalTime(),
                PublishedDate = publishDate,
                IsArchived = request.IsArchived,
                VisibleToRoles = request.VisibleToRoles,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _dbContext.NewsModel.Add(post);
            await _dbContext.SaveChangesAsync();
            _logger.LogDebug("Ny nyhet skapad med ID {Id}", post.Id);
            return Ok(post);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Samtidighetskonflikt vid skapande av nyhet");
            return Conflict(new { message = "Innehållet uppdaterades av någon annan. Försök igen." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid skapandet av ett nyhetsinlägg");
            return StatusCode(500, "Ett oväntat fel inträffade vid skapandet av nyhetsinlägget");
        }
    }

    // Uppdaterar ett befintligt nyhetsinlägg baserat på ID
    [HttpPut("update/{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateNewsPost(int id, [FromBody] UpdateNewsRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var post = await _dbContext.NewsModel.FindAsync(id);
            if (post == null)
            {
                _logger.LogWarning("Försök att uppdatera nyhetsinlägg som inte finns, ID: {Id}", id);
                return NotFound("Nyhetsinlägget hittades inte");
            }

            post.Title = request.Title;
            post.Content = request.Content;
            post.ScheduledPublishDate = request.ScheduledPublishDate;
            post.PublishedDate = request.ScheduledPublishDate ?? post.PublishedDate;
            post.IsArchived = request.IsArchived;
            post.VisibleToRoles = request.VisibleToRoles;
            post.LastModified = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            _logger.LogDebug("Nyhetsinlägg med ID {Id} uppdaterat", id);
            return Ok(post);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Samtidighetskonflikt vid uppdatering av nyhetsinlägg {Id}", id);
            return Conflict(new { message = "Innehållet uppdaterades av någon annan. Ladda om sidan och försök igen." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid uppdatering av nyhetsinlägg med ID {Id}", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid uppdatering av nyhetsinlägget");
        }
    }

    // Tar bort ett nyhetsinlägg permanent från databasen
    [HttpDelete("{id}")]
    [RequireRole("Admin")]
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

            _logger.LogDebug("Nyhetsinlägg med ID {Id} har raderats", id);
            return Ok("Nyhetsinlägget har tagits bort");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Samtidighetskonflikt vid borttagning av nyhetsinlägg {Id}", id);
            return Conflict(new { message = "Innehållet uppdaterades av någon annan. Försök igen." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid borttagning av nyhetsinlägg med ID {Id}", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid borttagning av nyhetsinlägget");
        }
    }

    // Hämtar nyheter för inloggade medlemmar (med rollfiltrering och publiceringsdatum)
    [HttpGet("member-news")]
    [RequireRole("Admin", "Styrelse", "Medlem")]
    public async Task<IActionResult> GetMemberNews([FromQuery] bool includeArchived = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            // Hämta användarroller från header
            var userRolesHeader = Request.Headers[RoleHeader].ToString();
            var userRoles = userRolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Lägg till grundläggande Member-roll om den inte finns
            if (!userRoles.Contains("Member") && !userRoles.Contains("Medlem"))
            {
                userRoles.Add("Member");
            }

            var now = DateTime.UtcNow;
            var query = _dbContext.NewsModel.AsQueryable();

            // Filtrera på publiceringsdatum (bara visa nyheter som ska vara synliga nu)
            query = query.Where(n => n.PublishedDate <= now);

            // Filtrera på roller - visa endast nyheter som användaren har rätt att se
            query = query.Where(n => userRoles.Any(role => n.VisibleToRoles.Contains(role)));

            // Filtrera arkiverade
            if (!includeArchived)
            {
                query = query.Where(n => !n.IsArchived);
            }

            // Sortera med nyaste först
            query = query.OrderByDescending(n => n.PublishedDate);

            // Paginering
            var totalCount = await query.CountAsync();
            var posts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new
            {
                Posts = posts,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            _logger.LogDebug("Medlemsnyheter hämtade: {Count} nyheter för roller {Roles}", posts.Count, string.Join(", ", userRoles));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid hämtning av medlemsnyheter");
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av nyheter");
        }
    }

    // Hämtar nyhetsinlägg med möjlighet att filtrera (för admin)
    [HttpGet("filtered")]
    [RequireRole("Admin")]
    public async Task<IActionResult> GetFilteredNews([FromQuery] bool? archived, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? roles)
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
            if (!string.IsNullOrEmpty(roles))
            {
                query = query.Where(n => n.VisibleToRoles.Contains(roles));
            }

            query = query.OrderByDescending(n => n.PublishedDate);

            var posts = await query.ToListAsync();
            _logger.LogDebug("Filtrerad nyhetslista hämtad med {Count} träffar", posts.Count);
            return Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid hämtning av filtrerade nyheter");
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av filtrerade nyheter");
        }
    }

    // Hämtar ett specifikt nyhetsinlägg baserat på ID (med rollkontroll)
    [HttpGet("{id}")]
    [RequireRole("Admin", "Styrelse", "Medlem")]
    public async Task<IActionResult> GetNewsPost(int id)
    {
        try
        {
            var post = await _dbContext.NewsModel.FindAsync(id);
            if (post == null)
            {
                return NotFound("Nyhetsinlägget hittades inte.");
            }

            // Kontrollera om användaren har rätt att se denna nyhet
            var userRolesHeader = Request.Headers[RoleHeader].ToString();
            var userRoles = userRolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Lägg till grundläggande Member-roll om den inte finns
            if (!userRoles.Contains("Member") && !userRoles.Contains("Medlem"))
            {
                userRoles.Add("Member");
            }

            var isAdmin = userRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
            var hasAccess = isAdmin || userRoles.Any(role => post.VisibleToRoles.Contains(role));

            if (!hasAccess)
            {
                _logger.LogWarning("Användare utan behörighet försökte läsa nyhet {Id}. Användarroller: {Roles}, krävda roller: {RequiredRoles}",
                    id, string.Join(", ", userRoles), post.VisibleToRoles);
                return Forbid("Du har inte behörighet att visa denna nyhet.");
            }

            // Kontrollera publiceringsdatum (admin kan se framtida nyheter)
            if (!isAdmin && post.PublishedDate > DateTime.UtcNow)
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

    // Hämtar alla nyhetsinlägg i systemet (endast admin)
    [HttpGet("all")]
    [RequireRole("Admin")]
    public async Task<IActionResult> GetAllNewsPosts()
    {
        try
        {
            var posts = await _dbContext.NewsModel
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
            return Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid hämtning av alla nyhetsinlägg.");
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av alla nyhetsinlägg.");
        }
    }

    // Arkiverar/avarkiverar en nyhet
    [HttpPut("{id}/archive")]
    [RequireRole("Admin")]
    public async Task<IActionResult> ToggleArchiveStatus(int id)
    {
        try
        {
            var post = await _dbContext.NewsModel.FindAsync(id);
            if (post == null)
            {
                return NotFound("Nyhetsinlägget hittades inte");
            }

            post.IsArchived = !post.IsArchived;
            post.LastModified = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            var status = post.IsArchived ? "arkiverad" : "avarkiverad";
            _logger.LogDebug("Nyhetsinlägg med ID {Id} {Status}", id, status);

            return Ok(new { message = $"Nyheten har {status}s", isArchived = post.IsArchived });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Samtidighetskonflikt vid arkivering/avarkivering av nyhetsinlägg {Id}", id);
            return Conflict(new { message = "Innehållet uppdaterades av någon annan. Ladda om och försök igen." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid arkivering av nyhetsinlägg med ID {Id}", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid arkivering av nyhetsinlägget");
        }
    }

    // Hämtar dokument för en nyhet
    [HttpGet("{newsId}/documents")]
    [RequireRole("Admin", "Styrelse", "Medlem")]
    public async Task<IActionResult> GetNewsDocuments(int newsId)
    {
        try
        {
            var userRoles = Request.Headers["X-User-Roles"].ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries);

            var newsItem = await _dbContext.NewsModel
                .Include(n => n.NewsDocuments)
                .ThenInclude(nd => nd.Document)
                .ThenInclude(d => d.MainCategory)
                .FirstOrDefaultAsync(n => n.Id == newsId);

            if (newsItem == null)
                return NotFound();

            // Kontrollera om användaren kan se nyheten
            if (!CanUserViewNews(newsItem, userRoles))
                return Forbid();

            // Filtrera dokument baserat på användarens roller
            var accessibleDocuments = new List<object>();

            foreach (var newsDoc in newsItem.NewsDocuments.OrderBy(nd => nd.SortOrder))
            {
                // Kontrollera om användaren har tillgång till dokumentet
                if (userRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase) ||
                    await _roleValidationService.UserHasAccessToCategoryAsync(newsDoc.Document.MainCategoryId, userRoles))
                {
                    accessibleDocuments.Add(new
                    {
                        Id = newsDoc.DocumentId,
                        DisplayName = newsDoc.DisplayName ?? newsDoc.Document.FileName,
                        FileName = newsDoc.Document.FileName,
                        FileSize = newsDoc.Document.FileSize,
                        SortOrder = newsDoc.SortOrder
                    });
                }
            }

            return Ok(accessibleDocuments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av dokument för nyhet {NewsId}", newsId);
            return StatusCode(500, "Ett fel inträffade vid hämtning av dokument");
        }
    }

    // Koppla dokument till nyhet
    [HttpPost("{newsId}/documents")]
    [RequireRole("Admin")]
    public async Task<IActionResult> AttachDocumentToNews(int newsId, [FromBody] AttachDocumentRequest request)
    {
        try
        {
            var newsItem = await _dbContext.NewsModel.FindAsync(newsId);
            if (newsItem == null)
                return NotFound("Nyheten hittades inte");

            var document = await _dbContext.Documents.FindAsync(request.DocumentId);
            if (document == null)
                return NotFound("Dokumentet hittades inte");

            // Kontrollera om kopplingen redan finns
            var existingLink = await _dbContext.NewsDocuments
                .FirstOrDefaultAsync(nd => nd.NewsId == newsId && nd.DocumentId == request.DocumentId);

            if (existingLink != null)
                return BadRequest("Dokumentet är redan kopplat till denna nyhet");

            var newsDocument = new NewsDocument
            {
                NewsId = newsId,
                DocumentId = request.DocumentId,
                DisplayName = request.DisplayName,
                SortOrder = request.SortOrder
            };

            _dbContext.NewsDocuments.Add(newsDocument);
            await _dbContext.SaveChangesAsync();

            return Ok("Dokumentet har kopplats till nyheten");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Samtidighetskonflikt vid koppling av dokument till nyhet {NewsId}", newsId);
            return Conflict(new { message = "Innehållet uppdaterades av någon annan. Försök igen." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid koppling av dokument till nyhet");
            return StatusCode(500, "Ett fel inträffade");
        }
    }

    // Ta bort dokument från nyhet
    [HttpDelete("{newsId}/documents/{documentId}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> DetachDocumentFromNews(int newsId, int documentId)
    {
        try
        {
            var newsDocument = await _dbContext.NewsDocuments
                .FirstOrDefaultAsync(nd => nd.NewsId == newsId && nd.DocumentId == documentId);

            if (newsDocument == null)
                return NotFound();

            _dbContext.NewsDocuments.Remove(newsDocument);
            await _dbContext.SaveChangesAsync();

            return Ok("Dokumentet har tagits bort från nyheten");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Samtidighetskonflikt vid borttagning av dokumentkoppling {DocumentId} från nyhet {NewsId}", documentId, newsId);
            return Conflict(new { message = "Innehållet uppdaterades av någon annan. Försök igen." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av dokumentkoppling");
            return StatusCode(500, "Ett fel inträffade");
        }
    }

    // Hjälpmetod för att kontrollera nyhetstillgång
    private bool CanUserViewNews(NewsModel news, string[] userRoles)
    {
        if (userRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            return true;

        var newsRoles = news.VisibleToRoles.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return newsRoles.Any(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }
}