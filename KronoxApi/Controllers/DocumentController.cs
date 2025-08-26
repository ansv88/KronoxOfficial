using KronoxApi.Attributes;
using KronoxApi.Data;
using KronoxApi.DTOs;
using KronoxApi.Models;
using KronoxApi.Requests;
using KronoxApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Controllers;

// API-kontroller för hantering av dokument och filuppladdning med rollbaserad åtkomst.
[ApiController]
[Route("api/documents")]
[RequireApiKey]
public class DocumentController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFileService _files;
    private readonly IRoleValidationService _roleValidationService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DocumentController> _log;

    public DocumentController(
        ApplicationDbContext db,
        IFileService files,
        IRoleValidationService roleValidationService,
        IWebHostEnvironment env,
        ILogger<DocumentController> log)
    {
        _db = db;
        _files = files;
        _roleValidationService = roleValidationService;
        _env = env;
        _log = log;
    }

    // Laddar upp ett dokument och kopplar det till kategorier.
    [HttpPost("upload")]
    [RequireRole("Admin")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadFileRequest req)
    {
        if (req.File == null || req.File.Length == 0)
            return BadRequest("Ingen fil vald.");

        var err = _files.ValidateDocument(req.File);
        if (err != null) return BadRequest(err);

        try
        {
            var mainCategory = await _db.MainCategories.FindAsync(req.MainCategoryId);
            if (mainCategory == null || !mainCategory.IsActive)
            {
                _log.LogWarning("Ogiltig eller inaktiv huvudkategori vid uppladdning: {MainCategoryId}", req.MainCategoryId);
                return BadRequest("Ogiltig eller inaktiv huvudkategori");
            }

            string categoryFolder = FormatFolderName(mainCategory.Name);
            var savedPath = await _files.SaveDocumentAsync(req.File, categoryFolder);

            var document = new Document
            {
                FileName = req.File.FileName,
                FilePath = savedPath,
                UploadedAt = DateTime.UtcNow,
                FileSize = req.File.Length,
                MainCategoryId = req.MainCategoryId,
                SubCategories = req.SubCategoryIds?.ToList() ?? new List<int>(),
                IsArchived = false
            };

            _db.Documents.Add(document);
            await _db.SaveChangesAsync();

            _log.LogInformation("Dokument uppladdat: {FileName} (ID: {Id})", document.FileName, document.Id);
            return Ok(document);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid uppladdning av dokument");
            return StatusCode(500, "Ett oväntat fel inträffade vid uppladdning av dokumentet.");
        }
    }

    // Returnerar en lista med alla dokument (endast admin).
    [HttpGet]
    [RequireRole("Admin")]
    public async Task<IActionResult> ListAll()
    {
        try
        {
            var allDocuments = await _db.Documents
                .Include(d => d.MainCategory)
                .Select(d => new DocumentDto
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    UploadedAt = d.UploadedAt,
                    FileSize = d.FileSize,
                    MainCategoryId = d.MainCategoryId,
                    SubCategories = d.SubCategories,
                    IsArchived = d.IsArchived,
                    ArchivedAt = d.ArchivedAt,
                    ArchivedBy = d.ArchivedBy,
                    MainCategory = new MainCategoryDto
                    {
                        Id = d.MainCategory.Id,
                        Name = d.MainCategory.Name,
                        AllowedRoles = d.MainCategory.AllowedRoles,
                        IsActive = d.MainCategory.IsActive
                    }
                })
                .ToListAsync();

            return Ok(allDocuments);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid hämtning av dokumentlista");
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av dokument.");
        }
    }

    // **NYCKELMETHOD** - Hämtar dokument baserat på användarens roller
    [HttpGet("accessible")]
    [RequireRole("Admin", "Styrelse", "Medlem")]
    public async Task<IActionResult> GetAccessibleDocuments()
    {
        try
        {
            var userRoles = Request.Headers["X-User-Roles"].ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries);

            // Admin kan se alla dokument
            if (userRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            {
                var allDocuments = await _db.Documents
                    .Include(d => d.MainCategory)
                    .Select(d => new DocumentDto
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        FilePath = d.FilePath,
                        UploadedAt = d.UploadedAt,
                        FileSize = d.FileSize,
                        MainCategoryId = d.MainCategoryId,
                        SubCategories = d.SubCategories,
                        IsArchived = d.IsArchived,
                        ArchivedAt = d.ArchivedAt,
                        ArchivedBy = d.ArchivedBy,
                        MainCategory = new MainCategoryDto
                        {
                            Id = d.MainCategory.Id,
                            Name = d.MainCategory.Name,
                            AllowedRoles = d.MainCategory.AllowedRoles,
                            IsActive = d.MainCategory.IsActive
                        }
                    })
                    .ToListAsync();
                return Ok(allDocuments);
            }

            // Hämta kategorier som användaren har tillgång till
            var accessibleCategoryIds = await _roleValidationService.GetAccessibleCategoryIdsAsync(userRoles);

            // Filtrera dokument baserat på kategoriroller och mappa till DTO
            var accessibleDocuments = await _db.Documents
                .Include(d => d.MainCategory)
                .Where(d => !d.IsArchived && 
                           accessibleCategoryIds.Contains(d.MainCategoryId))
                .Select(d => new DocumentDto
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    UploadedAt = d.UploadedAt,
                    FileSize = d.FileSize,
                    MainCategoryId = d.MainCategoryId,
                    SubCategories = d.SubCategories,
                    IsArchived = d.IsArchived,
                    ArchivedAt = d.ArchivedAt,
                    ArchivedBy = d.ArchivedBy,
                    MainCategory = new MainCategoryDto
                    {
                        Id = d.MainCategory.Id,
                        Name = d.MainCategory.Name,
                        AllowedRoles = d.MainCategory.AllowedRoles,
                        IsActive = d.MainCategory.IsActive
                    }
                })
                .ToListAsync();

            return Ok(accessibleDocuments);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid hämtning av tillgängliga dokument");
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av dokument.");
        }
    }

    // Streama ner dokumentet från PrivateStorage, med rollbaserad åtkomstkontroll
    [HttpGet("{id}")]
    [RequireRole("Admin", "Styrelse", "Medlem")]
    public async Task<IActionResult> Download(int id)
    {
        try
        {
            var document = await _db.Documents
                .Include(d => d.MainCategory)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                _log.LogWarning("Dokument med ID {Id} hittades inte vid nedladdning.", id);
                return NotFound();
            }

            // Kontrollera rollbaserad åtkomst
            var userRoles = Request.Headers["X-User-Roles"].ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries);

            // Admin kan ladda ner alla dokument
            if (!userRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            {
                // Kontrollera om användaren har tillgång till dokumentets kategori
                if (!await _roleValidationService.UserHasAccessToCategoryAsync(document.MainCategoryId, userRoles))
                {
                    _log.LogWarning("Användare utan behörighet försökte ladda ner dokument {Id}", id);
                    return Forbid("Du har inte behörighet att ladda ner detta dokument.");
                }

                // Kontrollera att dokumentet inte är arkiverat (utom för admin)
                if (document.IsArchived)
                {
                    _log.LogWarning("Försök att ladda ner arkiverat dokument {Id} av icke-admin", id);
                    return NotFound();
                }
            }

            var phys = Path.Combine(_env.ContentRootPath, "PrivateStorage", document.FilePath);
            if (!System.IO.File.Exists(phys))
            {
                _log.LogWarning("Filen saknas på disk: {FilePath}", phys);
                return NotFound("Filen saknas.");
            }

            Response.Headers["Cache-Control"] = "no-store";
            var ct = _files.GetContentType(document.FileName);
            return PhysicalFile(phys, ct, document.FileName);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid nedladdning av dokument med ID {Id}", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid nedladdning av dokumentet.");
        }
    }

    // Arkiverar ett dokument
    [HttpPost("{id}/archive")]
    [RequireRole("Admin")]
    public async Task<IActionResult> ArchiveDocument(int id)
    {
        try
        {
            var document = await _db.Documents.FindAsync(id);
            if (document == null)
            {
                _log.LogWarning("Dokument med ID {Id} hittades inte för arkivering.", id);
                return NotFound();
            }

            if (document.IsArchived)
            {
                return BadRequest("Dokumentet är redan arkiverat.");
            }

            document.IsArchived = true;
            document.ArchivedAt = DateTime.UtcNow;
            document.ArchivedBy = "Admin"; // Eller hämta från user context

            await _db.SaveChangesAsync();

            _log.LogInformation("Dokument arkiverat: {FileName} (ID: {Id}) av {User}", 
                document.FileName, document.Id, document.ArchivedBy);

            return Ok("Dokumentet har arkiverats.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid arkivering av dokument med ID {Id}", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid arkivering av dokumentet.");
        }
    }

    // Återställer ett arkiverat dokument
    [HttpPost("{id}/unarchive")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UnarchiveDocument(int id)
    {
        try
        {
            var document = await _db.Documents.FindAsync(id);
            if (document == null)
            {
                _log.LogWarning("Dokument med ID {Id} hittades inte för återställning.", id);
                return NotFound();
            }

            if (!document.IsArchived)
            {
                return BadRequest("Dokumentet är inte arkiverat.");
            }

            document.IsArchived = false;
            document.ArchivedAt = null;
            document.ArchivedBy = null;

            await _db.SaveChangesAsync();

            _log.LogInformation("Dokument återställt: {FileName} (ID: {Id})", 
                document.FileName, document.Id);

            return Ok("Dokumentet har återställts.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid återställning av dokument med ID {Id}", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid återställning av dokumentet.");
        }
    }

    // Uppdaterar huvudkategori och underkategorier för ett dokument.
    [HttpPut("{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> UpdateDocumentMainCategory(int id, [FromBody] UpdateDocumentRequest req)
    {
        try
        {
            var document = await _db.Documents.FindAsync(id);
            if (document == null)
            {
                _log.LogWarning("Dokument med ID {Id} hittades inte vid uppdatering.", id);
                return NotFound();
            }

            // Kontrollera att den nya huvudkategorin finns och är aktiv
            var newMainCategory = await _db.MainCategories.FindAsync(req.MainCategoryId);
            if (newMainCategory == null || !newMainCategory.IsActive)
            {
                _log.LogWarning("Ogiltig eller inaktiv ny huvudkategori vid uppdatering: {MainCategoryId}", req.MainCategoryId);
                return BadRequest("Ogiltig eller inaktiv ny huvudkategori");
            }

            var oldMainCategoryId = document.MainCategoryId;
            var oldFilePath = document.FilePath;

            document.MainCategoryId = req.MainCategoryId;
            if (req.SubCategoryIds != null)
            {
                document.SubCategories.Clear();
                document.SubCategories.AddRange(req.SubCategoryIds);
            }

            // Om huvudkategorin ändras, flytta filen
            if (oldMainCategoryId != req.MainCategoryId)
            {
                try
                {
                    string newCategoryFolder = FormatFolderName(newMainCategory.Name);
                    var sourceFilePath = Path.Combine(_env.ContentRootPath, "PrivateStorage", oldFilePath);
                    if (!System.IO.File.Exists(sourceFilePath))
                    {
                        _log.LogWarning("Källfilen hittades inte vid flytt: {SourceFilePath}", sourceFilePath);
                        return NotFound("Källfilen hittades inte. Kan inte flytta filen.");
                    }

                    var fileName = Path.GetFileName(oldFilePath);
                    var newRelativePath = Path.Combine("documents", newCategoryFolder, fileName);
                    var destDirPath = Path.Combine(_env.ContentRootPath, "PrivateStorage", "documents", newCategoryFolder);
                    var destFilePath = Path.Combine(destDirPath, fileName);

                    Directory.CreateDirectory(destDirPath);

                    if (System.IO.File.Exists(destFilePath))
                    {
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        var extension = Path.GetExtension(fileName);
                        var newFileName = $"{fileNameWithoutExt}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                        destFilePath = Path.Combine(destDirPath, newFileName);
                        newRelativePath = Path.Combine("documents", newCategoryFolder, newFileName).Replace("\\", "/");
                    }

                    System.IO.File.Copy(sourceFilePath, destFilePath);
                    System.IO.File.Delete(sourceFilePath);

                    document.FilePath = newRelativePath;

                    _log.LogInformation("Fil flyttad från {OldPath} till {NewPath}", oldFilePath, newRelativePath);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Misslyckades med att flytta fil för dokument med ID {Id}", id);
                    return StatusCode(500, "Kunde inte flytta filen: " + ex.Message);
                }
            }

            await _db.SaveChangesAsync();

            _log.LogInformation("Dokument uppdaterat: {FileName} (ID: {Id})", document.FileName, document.Id);

            return Ok(document);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid uppdatering av dokument med ID {Id}", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid uppdatering av dokumentet.");
        }
    }

    [HttpDelete("{id}")]
    [RequireRole("Admin")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        try
        {
            var document = await _db.Documents.FindAsync(id);
            if (document == null)
                return NotFound();

            // Ta bort filen från lagring
            await _files.DeleteDocumentAsync(document.FilePath);

            _db.Documents.Remove(document);
            await _db.SaveChangesAsync();

            _log.LogInformation("Dokument raderat: {FileName} (ID: {Id})", document.FileName, document.Id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid borttagning av dokument med ID {Id}", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid borttagning av dokumentet.");
        }
    }

    // Hjälpmetod för att formatera kategorinamn till giltiga mappnamn
    private string FormatFolderName(string categoryName)
    {
        var folderName = categoryName.ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("å", "a")
            .Replace("ä", "a")
            .Replace("ö", "o");

        folderName = string.Join("", folderName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return folderName;
    }
}