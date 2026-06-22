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

/// <summary>
/// API‑kontroller för innehållsblock och sidbilder (CMS).
/// Hämtar/skapar/uppdaterar ContentBlock per pageKey och hanterar PageImages:
/// lista/användning, uppladdning med deduplicering (hash‑suffix), registrering av befintliga filer,
/// uppdatering av alt‑text samt säker borttagning (endast när sista referensen försvunnit).
/// Adminkrav för ändringar, skyddad med API‑nyckel och __EnableRateLimiting("API")__
/// (samt "Upload" på bilduppladdning); returnerar 409 vid samtidighetskonflikt.
/// </summary>

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
                    AltText = pi.AltText,
                    PageKey = pi.PageKey,
                    IsActive = pi.IsActive
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
                AltText = pi.AltText,
                PageKey = pi.PageKey,
                IsActive = pi.IsActive
            }).ToList()
        };

        return Ok(resultDto);
    }

    private static string GetDefaultPageTitle(string pageKey)
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
        if (!ModelState.IsValid) return BadRequest(ModelState);

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
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Samtidighetskonflikt vid uppdatering av innehållsblock för {PageKey}", pageKey);
            return Conflict(new { message = "Innehållet uppdaterades av någon annan. Ladda om sidan och försök igen." });
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

            _logger.LogDebug("Alt-text uppdaterad för bild {Id} på {PageKey}", id, pageKey);
            return Ok();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Samtidighetskonflikt vid uppdatering av alt-text för bild {Id} på {PageKey}", id, pageKey);
            return Conflict(new { message = "Innehållet uppdaterades av någon annan. Ladda om sidan och försök igen." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av alt-text för bild {Id} på {PageKey}", id, pageKey);
            return StatusCode(500, "Ett oväntat fel inträffade vid uppdatering av alt-text.");
        }
    }

    // Lista alla bilder (bibliotek)
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
                AltText = pi.AltText,
                PageKey = pi.PageKey,
                IsActive = pi.IsActive
            })
            .ToListAsync();

        return Ok(items);
    }

    // Visa var en bild används (alla render-källor)
    [HttpGet("images/usage")]
    [RequireRole("Admin")]
    public async Task<IActionResult> GetImageUsage([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return BadRequest("url saknas");
        return Ok(await FindImageUsagesAsync(url));
    }

    // Användning för ALLA bilder i ett svep (för bildbibliotekets översikt)
    [HttpGet("images/usage-all")]
    [RequireRole("Admin")]
    public async Task<IActionResult> GetAllImageUsage()
    {
        var result = new List<ImageUsageDto>();

        foreach (var f in await _db.FeatureSections
            .Where(f => f.ImageUrl != "")
            .Select(f => new { f.PageKey, f.ImageUrl }).ToListAsync())
            result.Add(new ImageUsageDto { Url = f.ImageUrl, PageKey = f.PageKey, UsageType = "Funktionssektion", Description = $"Funktionssektion på {f.PageKey}" });

        foreach (var i in await _db.FaqItems.Include(i => i.FaqSection)
            .Where(i => i.ImageUrl != "")
            .Select(i => new { i.FaqSection.PageKey, i.ImageUrl }).ToListAsync())
            result.Add(new ImageUsageDto { Url = i.ImageUrl, PageKey = i.PageKey, UsageType = "FAQ", Description = $"FAQ på {i.PageKey}" });

        foreach (var b in await _db.ContentBlocks.Select(b => new { b.PageKey, b.Metadata }).ToListAsync())
        {
            if (string.IsNullOrWhiteSpace(b.Metadata)) continue;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(b.Metadata);
                if (doc.RootElement.TryGetProperty("introSection", out var intro) &&
                    intro.TryGetProperty("imageUrl", out var imgEl))
                {
                    var u = imgEl.GetString();
                    if (!string.IsNullOrWhiteSpace(u))
                        result.Add(new ImageUsageDto { Url = u, PageKey = b.PageKey, UsageType = "Introsektion", Description = $"Introsektion på {b.PageKey}" });
                }
            }
            catch (System.Text.Json.JsonException) { /* ignorera trasig metadata */ }
        }

        // Banner = aktiv PageImage som inte redan förklarats av en direkt källa
        var explained = new HashSet<string>(result.Select(r => $"{r.PageKey}|{r.Url}"), StringComparer.OrdinalIgnoreCase);
        foreach (var a in await _db.PageImages.Where(pi => pi.IsActive)
            .Select(pi => new { pi.Url, pi.PageKey }).ToListAsync())
            if (!explained.Contains($"{a.PageKey}|{a.Url}"))
                result.Add(new ImageUsageDto { Url = a.Url, PageKey = a.PageKey, UsageType = "Bannerbild", Description = $"Bannerbild på {a.PageKey}" });

        return Ok(result);
    }

    // Full skanning för EN url (inkl. inbäddade bilder) – används av delete-guarden
    private async Task<List<ImageUsageDto>> FindImageUsagesAsync(string url)
    {
        var usages = new List<ImageUsageDto>();
        if (string.IsNullOrWhiteSpace(url)) return usages;

        bool Has(string? s) => !string.IsNullOrEmpty(s) && s.Contains(url, StringComparison.OrdinalIgnoreCase);
        bool Eq(string? s) => string.Equals(s, url, StringComparison.OrdinalIgnoreCase);
        var explained = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string pageKey, string type, string desc) { usages.Add(new ImageUsageDto { Url = url, PageKey = pageKey, UsageType = type, Description = desc }); explained.Add($"{pageKey}|{url}"); }

        foreach (var f in await _db.FeatureSections.Select(f => new { f.PageKey, f.ImageUrl, f.Content }).ToListAsync())
            if (Eq(f.ImageUrl)) Add(f.PageKey, "Funktionssektion", $"Funktionssektion på {f.PageKey}");
            else if (Has(f.Content)) Add(f.PageKey, "Sidinnehåll", $"Inbäddad i funktionssektion på {f.PageKey}");

        foreach (var i in await _db.FaqItems.Include(i => i.FaqSection).Select(i => new { i.FaqSection.PageKey, i.ImageUrl, i.Answer }).ToListAsync())
            if (Eq(i.ImageUrl) || Has(i.Answer)) Add(i.PageKey, "FAQ", $"FAQ på {i.PageKey}");

        foreach (var b in await _db.ContentBlocks.Select(b => new { b.PageKey, b.Metadata, b.HtmlContent }).ToListAsync())
        {
            if (!string.IsNullOrWhiteSpace(b.Metadata))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(b.Metadata);
                    if (doc.RootElement.TryGetProperty("introSection", out var intro) &&
                        intro.TryGetProperty("imageUrl", out var imgEl) && Eq(imgEl.GetString()))
                        Add(b.PageKey, "Introsektion", $"Introsektion på {b.PageKey}");
                }
                catch (System.Text.Json.JsonException) { }
            }
            if (Has(b.HtmlContent)) Add(b.PageKey, "Sidinnehåll", $"Inbäddad i sidinnehåll på {b.PageKey}");
        }

        foreach (var pageKey in await _db.PageImages.Where(pi => pi.Url == url && pi.IsActive).Select(pi => pi.PageKey).ToListAsync())
            if (!explained.Contains($"{pageKey}|{url}"))
                Add(pageKey, "Bannerbild", $"Bannerbild på {pageKey}");

        return usages;
    }

    // Sätt IsActive för en bild och inaktivera övriga bilder med samma PageKey (om activate=true)
    [HttpPut("{pageKey}/images/{id}/active")]
    [RequireRole("Admin")]
    public async Task<IActionResult> SetImageActive(string pageKey, int id, [FromBody] SetImageActiveDto dto)
    {
        try
        {
            var image = await _db.PageImages.FirstOrDefaultAsync(pi => pi.Id == id && pi.PageKey == pageKey);
            if (image == null) return NotFound();

            if (dto.IsActive && dto.DeactivateOthers)
            {
                // Inaktivera alla övriga bilder på samma sida (banner-beteende)
                var others = await _db.PageImages
                    .Where(pi => pi.PageKey == pageKey && pi.Id != id && pi.IsActive)
                    .ToListAsync();
                foreach (var other in others)
                    other.IsActive = false;
            }

            image.IsActive = dto.IsActive;
            await _db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid uppdatering av IsActive för bild {Id} på {PageKey}", id, pageKey);
            return StatusCode(500, "Ett fel inträffade.");
        }
    }

    // Mappa MIME->ext (fallback)
    private static string GuessExtension(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/gif" => ".gif",
        "image/webp" => ".webp",
        "image/svg+xml" => ".svg",
        _ => ".jpg"
    };

    // Rensa filnamnsbas (för läsbart namn)
    private static string SanitizeBase(string name)
    {
        var baseName = Path.GetFileNameWithoutExtension(name)
            .Trim();
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "image";

        baseName = baseName.ToLowerInvariant();
        baseName = Regex.Replace(baseName, @"\s+", "-");            // mellanslag -> bindestreck
        baseName = Regex.Replace(baseName, @"[^a-z0-9\-_]+", "");   // endast a-z0-9-_ kvar
        if (baseName.Length > 40) baseName = baseName[..40];        // begränsa längd
        if (string.IsNullOrWhiteSpace(baseName)) baseName = "image";
        return baseName;
    }

    // Ta bort ev. tidigare hash-suffix (-[0-9a-f]{8}) i slutet av basnamnet
    private static string StripExistingHashSuffix(string baseName)
        => Regex.Replace(baseName, "-[0-9a-f]{8}$", "", RegexOptions.IgnoreCase);

    // SHA-256 -> 8 tecken (kort hash-suffix)
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

                return Ok(new PageImageDto { Id = pageImage.Id, Url = pageImage.Url, AltText = pageImage.AltText, PageKey = pageImage.PageKey, IsActive = pageImage.IsActive });
            }
            else
            {
                existing.AltText = altText;
                await _db.SaveChangesAsync();
                return Ok(new PageImageDto { Id = existing.Id, Url = existing.Url, AltText = existing.AltText, PageKey = existing.PageKey, IsActive = existing.IsActive });
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Samtidighetskonflikt vid uppladdning av bild för {PageKey}", pageKey);
            return Conflict(new { message = "Innehållet uppdaterades av någon annan. Ladda om sidan och försök igen." });
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
    public async Task<IActionResult> RegisterImageMetadata(string pageKey, [FromBody] RegisterExistingImageDto dto)
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

                return Ok(new PageImageDto { Id = pageImage.Id, Url = pageImage.Url, AltText = pageImage.AltText, PageKey = pageImage.PageKey, IsActive = pageImage.IsActive });
            }
            else
            {
                existing.AltText = dto.AltText;
                await _db.SaveChangesAsync();
                return Ok(new PageImageDto { Id = existing.Id, Url = existing.Url, AltText = existing.AltText, PageKey = existing.PageKey, IsActive = existing.IsActive });
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Samtidighetskonflikt vid registrering av bildmetadata för {PageKey}", pageKey);
            return Conflict(new { message = "Innehållet uppdaterades av någon annan. Ladda om sidan och försök igen." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid registrering av bildmetadata för {PageKey}", pageKey);
            return StatusCode(500, "Ett oväntat fel inträffade vid registrering av bildmetadata.");
        }
    }

    // SÄKER DELETE: blockera om bilden används; annars ta bort alla rader med samma URL + filen
    [HttpDelete("{pageKey}/images/{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> DeleteImage(string pageKey, int id)
    {
        try
        {
            var image = await _db.PageImages.FirstOrDefaultAsync(pi => pi.Id == id && pi.PageKey == pageKey);
            if (image == null) return NotFound();

            var usages = await FindImageUsagesAsync(image.Url);
            if (usages.Count > 0)
            {
                var where = string.Join(", ", usages.Select(u => u.Description).Distinct());
                return Conflict(new { message = $"Bilden används och kan inte tas bort: {where}. Ta bort den på respektive sida först." });
            }

            // Ta bort alla metadatarader som pekar på samma URL (biblioteks- + ev. referensrader)
            var rows = await _db.PageImages.Where(pi => pi.Url == image.Url).ToListAsync();
            _db.PageImages.RemoveRange(rows);
            await _db.SaveChangesAsync();

            if (!image.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var baseDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
                    + Path.DirectorySeparatorChar;
                var relPath = image.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var filePath = Path.GetFullPath(Path.Combine(baseDir, relPath));

                if (!filePath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                    _logger.LogWarning("Hoppar över radering utanför wwwroot: {Url}", image.Url);
                else if (System.IO.File.Exists(filePath))
                {
                    try { System.IO.File.Delete(filePath); }
                    catch (Exception ex) { _logger.LogError(ex, "Kunde inte ta bort filen {FilePath}", filePath); }
                }
            }

            return Ok();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Samtidighetskonflikt vid borttagning av bild {Id} för {PageKey}", id, pageKey);
            return Conflict(new { message = "Innehållet uppdaterades av någon annan. Ladda om sidan och försök igen." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid borttagning av bild {Id} för {PageKey}", id, pageKey);
            return StatusCode(500, "Ett oväntat fel inträffade vid borttagning av bilden.");
        }
    }
}