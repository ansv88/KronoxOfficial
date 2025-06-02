using KronoxApi.Attributes;
using KronoxApi.Data;
using KronoxApi.DTOs;
using KronoxApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Controllers;

// API-kontroller för hantering av innehållsblock och sidbilder.
[ApiController]
[Route("api/content")]
[RequireApiKey]
public class ContentController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ContentController> _logger;

    public ContentController(ApplicationDbContext db, ILogger<ContentController> logger)
    {
        _db = db;
        _logger = logger;
    }


    // Hämtar ett innehållsblock baserat på pageKey.
    [HttpGet("{pageKey}")]
    public async Task<IActionResult> GetContent(string pageKey)
    {
        var block = await _db.ContentBlocks
            .Include(cb => cb.PageImages)
            .FirstOrDefaultAsync(cb => cb.PageKey == pageKey);

        if (block == null)
            return NotFound();

        var dto = new ContentBlockDto
        {
            PageKey = block.PageKey,
            Title = block.Title,
            HtmlContent = block.HtmlContent,
            Metadata = block.Metadata,
            LastModified = block.LastModified,
            Images = block.PageImages
                               .Select(pi => new PageImageDto
                               {
                                   Id = pi.Id,
                                   Url = pi.Url,
                                   AltText = pi.AltText
                               }).ToList()
        };

        return Ok(dto);
    }


    // Skapar eller uppdaterar ett innehållsblock baserat på pageKey.
    [HttpPut("{pageKey}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateContent(string pageKey, [FromBody] ContentBlockDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var block = await _db.ContentBlocks
                .FirstOrDefaultAsync(cb => cb.PageKey == pageKey);

            if (block == null)
            {
                block = new ContentBlock
                {
                    PageKey = pageKey,
                    Title = dto.Title,
                    HtmlContent = dto.HtmlContent,
                    Metadata = dto.Metadata,
                    LastModified = DateTime.UtcNow
                };
                _db.ContentBlocks.Add(block);
            }
            else
            {
                block.Title = dto.Title;
                block.HtmlContent = dto.HtmlContent;
                block.Metadata = dto.Metadata;
                block.LastModified = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av innehållsblock för {PageKey}", pageKey);
            return StatusCode(500, "Ett oväntat fel inträffade vid uppdatering av innehållsblocket.");
        }
    }


    // Uppdaterar alt-texten för en bild kopplad till ett innehållsblock.
    [HttpPut("{pageKey}/images/{id}/alttext")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateImageAltText(string pageKey, int id, [FromBody] dynamic dto)
    {
        try
        {
            var pageImage = await _db.PageImages
                .FirstOrDefaultAsync(pi => pi.Id == id && pi.PageKey == pageKey);

            if (pageImage == null)
            {
                _logger.LogWarning("Bild med ID {Id} och pageKey {PageKey} hittades inte för alt-textuppdatering.", id, pageKey);
                return NotFound();
            }

            pageImage.AltText = dto.AltText.ToString();
            await _db.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av alt-text för bild {Id} på {PageKey}", id, pageKey);
            return StatusCode(500, "Ett oväntat fel inträffade vid uppdatering av alt-text.");
        }
    }


    // Laddar upp en ny bild och kopplar den till ett innehållsblock.
    [HttpPost("{pageKey}/images")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UploadImage(string pageKey)
    {
        try
        {
            if (!Request.HasFormContentType || !Request.Form.Files.Any())
                return BadRequest("Ingen fil hittades.");

            var file = Request.Form.Files[0];
            var altText = Request.Form["altText"].ToString() ?? "";

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest("Filtypen stöds inte. Använd JPG, PNG, GIF, WebP eller SVG.");

            var uploadsDir = Path.Combine("content", "images", "pages", pageKey);
            var fullDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", uploadsDir);
            Directory.CreateDirectory(fullDir);

            var fileName = Path.GetFileName(file.FileName);
            var safeName = $"{DateTime.Now:yyyyMMddHHmmss}-{Path.GetFileNameWithoutExtension(fileName)}{Path.GetExtension(fileName)}";
            var filePath = Path.Combine(fullDir, safeName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Hitta eller skapa ContentBlock
            var block = await _db.ContentBlocks.FirstOrDefaultAsync(cb => cb.PageKey == pageKey);
            if (block == null)
            {
                block = new ContentBlock { PageKey = pageKey, Title = pageKey };
                _db.ContentBlocks.Add(block);
                await _db.SaveChangesAsync();
            }

            // Spara metadata i databasen
            var pageImage = new PageImage
            {
                Url = $"/{uploadsDir}/{safeName}",
                AltText = altText,
                PageKey = pageKey
            };

            _db.PageImages.Add(pageImage);
            await _db.SaveChangesAsync();

            var dto = new PageImageDto
            {
                Id = pageImage.Id,
                Url = pageImage.Url,
                AltText = pageImage.AltText
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppladdning av bild");
            return StatusCode(500, "Ett serverfel inträffade vid uppladdning av bilden.");
        }
    }


    // Registrerar metadata för en redan uppladdad bild.
    [HttpPost("{pageKey}/images/register")]
    [RequireRole("Admin")]
    public async Task<IActionResult> RegisterImageMetadata(string pageKey, [FromBody] RegisterPageImageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SourcePath))
            return BadRequest("Source path kan inte vara tom.");

        try
        {
            var sourcePath = dto.SourcePath.TrimStart('/');
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", sourcePath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound($"Filen hittades inte: {sourcePath}");

            var block = await _db.ContentBlocks.FirstOrDefaultAsync(cb => cb.PageKey == pageKey);
            if (block == null)
            {
                block = new ContentBlock { PageKey = pageKey, Title = pageKey };
                _db.ContentBlocks.Add(block);
                await _db.SaveChangesAsync();
            }

            var pageImage = new PageImage
            {
                Url = "/" + sourcePath,
                AltText = dto.AltText,
                PageKey = pageKey
            };

            _db.PageImages.Add(pageImage);
            await _db.SaveChangesAsync();

            var responseDto = new PageImageDto
            {
                Id = pageImage.Id,
                Url = pageImage.Url,
                AltText = pageImage.AltText
            };

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid registrering av bildmetadata för {PageKey}", pageKey);
            return StatusCode(500, "Ett oväntat fel inträffade vid registrering av bildmetadata.");
        }
    }


    // Tar bort en bild och dess metadata.
    [HttpDelete("{pageKey}/images/{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> DeleteImage(string pageKey, int id)
    {
        try
        {
            var image = await _db.PageImages.FirstOrDefaultAsync(
                pi => pi.Id == id && pi.PageKey == pageKey);

            if (image == null)
            {
                _logger.LogWarning("Bild med ID {Id} och pageKey {PageKey} hittades inte för borttagning.", id, pageKey);
                return NotFound();
            }

            // Ta bort filen om den är lagrad lokalt (innehåller inte http:// eller https://)
            if (!image.Url.StartsWith("http"))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                                             image.Url.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Kunde inte ta bort filen {FilePath}", filePath);
                        // Fortsätt ändå och ta bort från databasen
                    }
                }
            }

            _db.PageImages.Remove(image);
            await _db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av bild {Id} för {PageKey}", id, pageKey);
            return StatusCode(500, "Ett oväntat fel inträffade vid borttagning av bilden.");
        }
    }
}