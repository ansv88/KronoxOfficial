using KronoxApi.Attributes;
using KronoxApi.Data;
using KronoxApi.Models;
using KronoxApi.Requests;
using KronoxApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Controllers;

// API-kontroller för hantering av dokument och filuppladdning.
[ApiController]
[Route("api/documents")]
[RequireApiKey]
public class DocumentController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFileService _files;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DocumentController> _log;

    public DocumentController(
        ApplicationDbContext db,
        IFileService files,
        IWebHostEnvironment env,
        ILogger<DocumentController> log)
    {
        _db = db;
        _files = files;
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
            if (mainCategory == null)
            {
                _log.LogWarning("Ogiltig huvudkategori vid uppladdning: {MainCategoryId}", req.MainCategoryId);
                return BadRequest("Ogiltig huvudkategori");
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
                SubCategories = req.SubCategoryIds?.ToList() ?? new List<int>()
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

    // Returnerar en lista med alla dokument.
    [HttpGet]
    [RequireRole("Admin")]
    public async Task<IActionResult> ListAll()
    {
        try
        {
            var allDocuments = await _db.Documents.ToListAsync();
            return Ok(allDocuments);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid hämtning av dokumentlista");
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av dokument.");
        }
    }


    // Streama ner dokumentet från PrivateStorage, med åtkomstkontroll
    [HttpGet("{id}")]
    [RequireRole("Admin", "Styrelse", "Medlem")]
    public async Task<IActionResult> Download(int id)
    {
        try
        {
            var document = await _db.Documents.FindAsync(id);
            if (document == null)
            {
                _log.LogWarning("Dokument med ID {Id} hittades inte vid nedladdning.", id);
                return NotFound();
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


    // Hämtar dokument kopplade till en viss huvudkategori.
    [HttpGet("by-maincategory/{mainCategoryId}")]
    [Authorize]
    public async Task<IActionResult> GetByMainCategory(int mainCategoryId)
    {
        try
        {
            var documents = await _db.Documents
                .Where(d => d.MainCategoryId == mainCategoryId)
                .ToListAsync();

            return Ok(documents);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid hämtning av dokument för huvudkategori {MainCategoryId}", mainCategoryId);
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av dokument.");
        }
    }


    // Hämtar dokument kopplade till en viss underkategori.
    [HttpGet("by-subcategory/{subCategoryId}")]
    [Authorize]
    public async Task<IActionResult> GetBySubCategory(int subCategoryId)
    {
        try
        {
            var documents = await _db.Documents
                .Where(d => d.SubCategories.Contains(subCategoryId))
                .ToListAsync();

            return Ok(documents);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid hämtning av dokument för underkategori {SubCategoryId}", subCategoryId);
            return StatusCode(500, "Ett oväntat fel inträffade vid hämtning av dokument.");
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
                    var newMainCategory = await _db.MainCategories.FindAsync(req.MainCategoryId);
                    if (newMainCategory == null)
                    {
                        _log.LogWarning("Ogiltig ny huvudkategori vid uppdatering: {MainCategoryId}", req.MainCategoryId);
                        return BadRequest("Ogiltig ny huvudkategori");
                    }

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
            return Ok(document);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid uppdatering av dokument med ID {Id}", id);
            return StatusCode(500, "Ett oväntat fel inträffade vid uppdatering av dokumentet.");
        }
    }


    // Hjälpmetod för att formatera kategorinamn till giltiga mappnamn
    private string FormatFolderName(string categoryName)
    {
        // Konvertera till lowercase och ersätt ogiltiga tecken med understreck
        var folderName = categoryName.ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("å", "a")
            .Replace("ä", "a")
            .Replace("ö", "o");

        // Ta bort ogiltiga tecken för filsystem
        folderName = string.Join("", folderName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return folderName;
    }
}