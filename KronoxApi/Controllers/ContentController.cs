using KronoxApi.Attributes;
using KronoxApi.Data;
using KronoxApi.DTOs;
using KronoxApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace KronoxApi.Controllers;

// API-kontroller för hantering av innehållsblock och sidbilder.
[ApiController]
[Route("api/content")]
[RequireApiKey]
[EnableRateLimiting("API")]
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

        // Om inget ContentBlock finns, skapa ett tomt men inkludera alla PageImages för sidan
        if (block == null)
        {
            // Hämta alla bilder för denna pageKey direkt från PageImages-tabellen
            var pageImages = await _db.PageImages
                .Where(pi => pi.PageKey == pageKey)
                .ToListAsync();

            var dto = new ContentBlockDto
            {
                PageKey = pageKey,
                Title = GetDefaultPageTitle(pageKey),
                HtmlContent = "",
                Metadata = "{}",
                LastModified = DateTime.UtcNow,
                Images = pageImages.Select(pi => new PageImageDto
                {
                    Id = pi.Id,
                    Url = pi.Url,
                    AltText = pi.AltText
                }).ToList()
            };

            return Ok(dto);
        }

        // Returnera befintligt block med dess bilder
        var resultDto = new ContentBlockDto
        {
            PageKey = block.PageKey,
            Title = block.Title,
            HtmlContent = block.HtmlContent,
            Metadata = block.Metadata,
            LastModified = block.LastModified,
            Images = block.PageImages.Select(pi => new PageImageDto
            {
                Id = pi.Id,
                Url = pi.Url,
                AltText = pi.AltText
            }).ToList()
        };

        return Ok(resultDto);
    }

    private string GetDefaultPageTitle(string pageKey)
    {
        return pageKey switch
        {
            "home" => "Startsida",
            "omkonsortiet" => "Om konsortiet",
            "visioner" => "Visioner & Verksamhetsidé",
            _ => "Sida"
        };
    }


    // Skapar eller uppdaterar ett innehållsblock baserat på pageKey
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
    public async Task<IActionResult> UpdateImageAltText(string pageKey, int id, [FromBody] UpdateAltTextDto dto)
    {
        try
        {
            if (dto == null || string.IsNullOrEmpty(dto.AltText))
            {
                return BadRequest("AltText är obligatoriskt.");
            }

            var pageImage = await _db.PageImages
                .FirstOrDefaultAsync(pi => pi.Id == id && pi.PageKey == pageKey);

            if (pageImage == null)
            {
                _logger.LogWarning("Bild med ID {Id} och pageKey {PageKey} hittades inte för alt-textuppdatering.", id, pageKey);
                return NotFound();
            }

            pageImage.AltText = dto.AltText;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Alt-text uppdaterad för bild {Id} på {PageKey}", id, pageKey);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av alt-text för bild {Id} på {PageKey}", id, pageKey);
            return StatusCode(500, "Ett oväntat fel inträffade vid uppdatering av alt-text.");
        }
    }


    // NY: Lista alla bilder (bibliotek)
    [HttpGet("images")]
    [RequireRole("Admin")]
    public async Task<IActionResult> GetAllImages([FromQuery] string? pageKey = null)
    {
        var q = _db.PageImages.AsQueryable();
        if (!string.IsNullOrWhiteSpace(pageKey))
            q = q.Where(pi => pi.PageKey == pageKey);

        var items = await q.OrderByDescending(pi => pi.Id)
            .Select(pi => new PageImageDto
            {
                Id = pi.Id,
                Url = pi.Url,
                AltText = pi.AltText
            })
            .ToListAsync();

        return Ok(items);
    }

    // NY: Visa var en bild används (alla referenser till samma Url)
    [HttpGet("images/usage")]
    [RequireRole("Admin")]
    public async Task<IActionResult> GetImageUsage([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return BadRequest("url saknas");
        var usage = await _db.PageImages
            .Where(pi => pi.Url == url)
            .Select(pi => new { pi.Id, pi.PageKey, pi.AltText })
            .ToListAsync();
        return Ok(usage);
    }

    // NY: Mappa MIME->ext (fallback)
    private static string GuessExtension(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/gif" => ".gif",
        "image/webp" => ".webp",
        "image/svg+xml" => ".svg",
        _ => ".jpg"
    };

    // NY: Rensa filnamnsbas (för läsbart namn)
    private static string SanitizeBase(string name)
    {
        var baseName = Path.GetFileNameWithoutExtension(name)
            .Trim();
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "image";

        baseName = baseName.ToLowerInvariant();
        baseName = Regex.Replace(baseName, @"\s+", "-");           // mellanslag -> bindestreck
        baseName = Regex.Replace(baseName, @"[^a-z0-9\-_]+", "");   // endast a-z0-9-_ kvar
        if (baseName.Length > 40) baseName = baseName[..40];        // begränsa längd
        if (string.IsNullOrWhiteSpace(baseName)) baseName = "image";
        return baseName;
    }

    // NY: Strippa ev. tidigare hash-suffix (-[0-9a-f]{8}) i slutet av basnamnet
    private static string StripExistingHashSuffix(string baseName)
        => Regex.Replace(baseName, "-[0-9a-f]{8}$", "", RegexOptions.IgnoreCase);

    // NY: SHA-256 -> 8 tecken (kort hash-suffix)
    private static string ShortHash8(Stream s)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(s);
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return hex[..8];
    }

    // Ladda upp bild med dedupe + kort hash-suffix
    [HttpPost("{pageKey}/images")]
    [RequireRole("Admin")]
    [EnableRateLimiting("Upload")]
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

            var uploadsDir = Path.Combine("images", "pages", pageKey);
            var fullDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", uploadsDir);
            Directory.CreateDirectory(fullDir);

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext))
                ext = GuessExtension(file.ContentType);

            var baseName = SanitizeBase(file.FileName);
            baseName = StripExistingHashSuffix(baseName); // undvik dubbelhash

            // Hasha innehållet
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            var hash8 = ShortHash8(ms);
            ms.Position = 0;

            // Om fil med samma hash redan finns i mappen: återanvänd den filens namn/URL
            string? existingPath = Directory.EnumerateFiles(fullDir, $"*-{hash8}.*").FirstOrDefault();
            string finalName;
            if (existingPath != null)
            {
                finalName = Path.GetFileName(existingPath);
            }
            else
            {
                // Skriv ny fil med basnamn-hash8.ext
                finalName = $"{baseName}-{hash8}{ext}".ToLowerInvariant();
                var targetPath = Path.Combine(fullDir, finalName);
                await using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await ms.CopyToAsync(fs);
            }

            var url = $"/{uploadsDir}/{finalName}".Replace("\\", "/");

            // Säkerställ ContentBlock
            var block = await _db.ContentBlocks.FirstOrDefaultAsync(cb => cb.PageKey == pageKey);
            if (block == null)
            {
                block = new ContentBlock { PageKey = pageKey, Title = pageKey };
                _db.ContentBlocks.Add(block);
                await _db.SaveChangesAsync();
            }

            // Upsert metadata: undvik dublett för samma pageKey+Url
            var existing = await _db.PageImages.FirstOrDefaultAsync(pi => pi.PageKey == pageKey && pi.Url == url);
            if (existing is null)
            {
                var pageImage = new PageImage { Url = url, AltText = altText, PageKey = pageKey };
                _db.PageImages.Add(pageImage);
                await _db.SaveChangesAsync();

                return Ok(new PageImageDto { Id = pageImage.Id, Url = pageImage.Url, AltText = pageImage.AltText });
            }
            else
            {
                existing.AltText = altText;
                await _db.SaveChangesAsync();
                return Ok(new PageImageDto { Id = existing.Id, Url = existing.Url, AltText = existing.AltText });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppladdning av bild för {PageKey}", pageKey);
            return StatusCode(500, "Ett serverfel inträffade vid uppladdning av bilden.");
        }
    }


    // Registrerar metadata för en redan uppladdad bild (utan kopiering).
    [HttpPost("{pageKey}/images/register")]
    [RequireRole("Admin")]
    public async Task<IActionResult> RegisterImageMetadata(string pageKey, [FromBody] RegisterExistingImageRequest dto)
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

            var url = ("/" + sourcePath).Replace("\\", "/");

            var existing = await _db.PageImages.FirstOrDefaultAsync(pi => pi.PageKey == pageKey && pi.Url == url);
            if (existing is null)
            {
                var pageImage = new PageImage { Url = url, AltText = dto.AltText, PageKey = pageKey };
                _db.PageImages.Add(pageImage);
                await _db.SaveChangesAsync();

                return Ok(new PageImageDto { Id = pageImage.Id, Url = pageImage.Url, AltText = pageImage.AltText });
            }
            else
            {
                existing.AltText = dto.AltText;
                await _db.SaveChangesAsync();
                return Ok(new PageImageDto { Id = existing.Id, Url = existing.Url, AltText = existing.AltText });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid registrering av bildmetadata för {PageKey}", pageKey);
            return StatusCode(500, "Ett oväntat fel inträffade vid registrering av bildmetadata.");
        }
    }

    // Kopierar en redan uppladdad bild till rätt sidmapp och registrerar den
    [HttpPost("{pageKey}/images/register-copy")]
    [RequireRole("Admin")]
    public async Task<IActionResult> RegisterImageCopy(string pageKey, [FromBody] RegisterExistingImageRequest dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.SourcePath))
            return BadRequest("Source path kan inte vara tom.");

        try
        {
            var sourceRel = dto.SourcePath.TrimStart('/');
            var sourceFull = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", sourceRel);
            if (!System.IO.File.Exists(sourceFull))
                return NotFound($"Filen hittades inte: {sourceRel}");

            // Target directory for this page
            var uploadsDir = Path.Combine("images", "pages", pageKey);
            var targetDirFull = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", uploadsDir);
            Directory.CreateDirectory(targetDirFull);

            // Build name based on source file and content hash
            var ext = Path.GetExtension(sourceFull);
            if (string.IsNullOrWhiteSpace(ext))
                ext = GuessExtension("image/jpeg"); // fallback

            var baseName = SanitizeBase(Path.GetFileName(sourceFull));
            baseName = StripExistingHashSuffix(baseName); // undvik dubbelhash

            await using var ms = new MemoryStream(await System.IO.File.ReadAllBytesAsync(sourceFull));
            ms.Position = 0;
            var hash8 = ShortHash8(ms);
            ms.Position = 0;

            // If file with same content exists in target folder, reuse; otherwise copy
            var existingPath = Directory.EnumerateFiles(targetDirFull, $"*-{hash8}.*").FirstOrDefault();
            string finalName;
            if (existingPath != null)
            {
                finalName = Path.GetFileName(existingPath);
            }
            else
            {
                finalName = $"{baseName}-{hash8}{ext}".ToLowerInvariant();
                var targetFull = Path.Combine(targetDirFull, finalName);
                await using var outStream = new FileStream(targetFull, FileMode.Create, FileAccess.Write, FileShare.None);
                await ms.CopyToAsync(outStream);
            }

            var url = $"/{uploadsDir}/{finalName}".Replace("\\", "/");

            // Säkerställ ContentBlock
            var block = await _db.ContentBlocks.FirstOrDefaultAsync(cb => cb.PageKey == pageKey);
            if (block == null)
            {
                block = new ContentBlock { PageKey = pageKey, Title = pageKey };
                _db.ContentBlocks.Add(block);
                await _db.SaveChangesAsync();
            }

            // Register (upsert) PageImage
            var existing = await _db.PageImages.FirstOrDefaultAsync(pi => pi.PageKey == pageKey && pi.Url == url);
            if (existing is null)
            {
                var pageImage = new PageImage { Url = url, AltText = dto.AltText, PageKey = pageKey };
                _db.PageImages.Add(pageImage);
                await _db.SaveChangesAsync();
                return Ok(new PageImageDto { Id = pageImage.Id, Url = pageImage.Url, AltText = pageImage.AltText });
            }
            else
            {
                existing.AltText = dto.AltText;
                await _db.SaveChangesAsync();
                return Ok(new PageImageDto { Id = existing.Id, Url = existing.Url, AltText = existing.AltText });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid kopiering av bild till {PageKey}", pageKey);
            return StatusCode(500, "Ett oväntat fel inträffade vid kopiering av bild.");
        }
    }

    // SÄKER DELETE: ta bort fysisk fil endast när sista referensen försvunnit
    [HttpDelete("{pageKey}/images/{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> DeleteImage(string pageKey, int id)
    {
        try
        {
            var image = await _db.PageImages.FirstOrDefaultAsync(pi => pi.Id == id && pi.PageKey == pageKey);
            if (image == null) return NotFound();

            _db.PageImages.Remove(image);
            await _db.SaveChangesAsync();

            var stillReferenced = await _db.PageImages.AnyAsync(pi => pi.Url == image.Url);
            if (!stillReferenced && !image.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.Url.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    try { System.IO.File.Delete(filePath); }
                    catch (Exception ex) { _logger.LogError(ex, "Kunde inte ta bort filen {FilePath}", filePath); }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av bild {Id} för {PageKey}", id, pageKey);
            return StatusCode(500, "Ett oväntat fel inträffade vid borttagning av bilden.");
        }
    }
}