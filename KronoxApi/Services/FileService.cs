using System.Net.Mime;
using KronoxApi.Utilities;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Services;

// Tjänst för filhantering, validering och lagring.
public class FileService : IFileService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileService> _log;
    private const string PrivateRoot = "PrivateStorage";

    public FileService(IWebHostEnvironment env, ILogger<FileService> log)
    {
        _env = env;
        _log = log;
    }

    // Validerar en bildfil. Returnerar null om giltig, annars felmeddelande.
    public string? ValidateImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return "Ingen fil skickades med.";

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!FileValidation.AllowedImageExtensions.Contains(ext))
            return $"Ogiltigt bildformat. Tillåtna format: {string.Join(", ", FileValidation.AllowedImageExtensions)}";

        if (file.Length > FileValidation.MaxImageSize)
            return $"Bilden är för stor. Maxstorlek: {FileValidation.MaxImageSize / (1024 * 1024)} MB.";

        return null;
    }

    // Validerar ett dokument. Returnerar null om giltig, annars felmeddelande.
    public string? ValidateDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return "Ingen fil skickades med.";

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!FileValidation.AllowedDocumentExtensions.Contains(ext))
            return $"Ogiltigt filformat. Tillåtna format: {string.Join(", ", FileValidation.AllowedDocumentExtensions)}";

        if (file.Length > FileValidation.MaxDocumentSize)
            return $"Filen är för stor. Maxstorlek: {FileValidation.MaxDocumentSize / (1024 * 1024)} MB.";

        return null;
    }

    // Sparar ett dokument i angiven kategori. Returnerar relativ sökväg.
    public async Task<string> SaveDocumentAsync(IFormFile file, string category)
    {
        try
        {
            var relDir = Path.Combine("documents", category);
            var absDir = Path.Combine(_env.ContentRootPath, PrivateRoot, relDir);
            Directory.CreateDirectory(absDir);

            var originalFileName = Path.GetFileName(file.FileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var extension = Path.GetExtension(originalFileName);

            var fileName = originalFileName;
            var filePath = Path.Combine(absDir, fileName);

            if (System.IO.File.Exists(filePath))
            {
                fileName = $"{fileNameWithoutExtension}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                filePath = Path.Combine(absDir, fileName);
                _log.LogWarning("Filnamnskonflikt, nytt filnamn genererat: {FileName}", fileName);
            }

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            _log.LogInformation("Dokument sparat: {FileName}", fileName);
            return Path.Combine(relDir, fileName).Replace("\\", "/");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid sparande av dokument: {FileName}", file?.FileName);
            throw;
        }
    }

    // Raderar ett dokument från lagringen.
    public Task DeleteDocumentAsync(string relativePath)
    {
        try
        {
            var absPath = Path.Combine(_env.ContentRootPath, PrivateRoot,
                relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (File.Exists(absPath))
            {
                File.Delete(absPath);
                _log.LogInformation("Dokument raderat: {Path}", absPath);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid radering av dokument: {Path}", relativePath);
        }
        return Task.CompletedTask;
    }

    // Returnerar content-type för en given fil.
    public string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => MediaTypeNames.Application.Pdf,
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => MediaTypeNames.Text.Plain,
            _ => MediaTypeNames.Application.Octet
        };
    }

    // Sparar en sidbild.
    public async Task<string> SavePageImageAsync(IFormFile file, string pageKey, bool preserveFilename)
    {
        try
        {
            var relDir = Path.Combine("images", "pages", pageKey);
            var absDir = Path.Combine(_env.WebRootPath, relDir);
            Directory.CreateDirectory(absDir);

            var fn = preserveFilename
                ? file.FileName
                : $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var absPath = Path.Combine(absDir, fn);
            await using var stream = new FileStream(absPath, FileMode.Create);
            await file.CopyToAsync(stream);

            _log.LogInformation("Sidbild sparad: {FileName}", fn);
            return $"/{relDir.Replace("\\", "/")}/{fn}";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid sparande av sidbild: {FileName}", file?.FileName);
            throw;
        }
    }

    // Raderar en sidbild.
    public Task DeletePageImageAsync(string imageUrl)
    {
        try
        {
            var relPath = imageUrl.TrimStart('/');
            var absPath = Path.Combine(_env.WebRootPath, relPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (File.Exists(absPath))
            {
                File.Delete(absPath);
                _log.LogInformation("Sidbild raderad: {Path}", absPath);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid radering av sidbild: {Path}", imageUrl);
        }
        return Task.CompletedTask;
    }

    // Sparar en feature-bild (startsidan).
    public async Task<string> SaveFeatureImageAsync(IFormFile file, string indexIdentifier, bool preserveFilename)
    {
        try
        {
            var relDir = Path.Combine("images", "features", indexIdentifier);
            var absDir = Path.Combine(_env.WebRootPath, relDir);
            Directory.CreateDirectory(absDir);

            var fn = preserveFilename
                ? file.FileName
                : $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var absPath = Path.Combine(absDir, fn);
            await using var stream = new FileStream(absPath, FileMode.Create);
            await file.CopyToAsync(stream);

            _log.LogInformation("Feature-bild sparad: {FileName}", fn);
            return $"/{relDir.Replace("\\", "/")}/{fn}";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid sparande av feature-bild: {FileName}", file?.FileName);
            throw;
        }
    }

    // Raderar en feature-bild.
    public Task DeleteFeatureImageAsync(string imageUrl)
    {
        try
        {
            var relPath = imageUrl.TrimStart('/');
            var absPath = Path.Combine(_env.WebRootPath, relPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (File.Exists(absPath))
            {
                File.Delete(absPath);
                _log.LogInformation("Feature-bild raderad: {Path}", absPath);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid radering av feature-bild: {Path}", imageUrl);
        }
        return Task.CompletedTask;
    }

    // Sparar en medlemslogotyp.
    public async Task<string> SaveMemberLogoAsync(IFormFile file)
    {
        try
        {
            var relDir = Path.Combine("images", "members");
            var absDir = Path.Combine(_env.WebRootPath, relDir);
            Directory.CreateDirectory(absDir);

            var fn = Path.GetFileName(file.FileName);
            var absPath = Path.Combine(absDir, fn);

            if (File.Exists(absPath))
            {
                _log.LogInformation("Medlemslogotyp finns redan: {FileName}, sparar inte om.", fn);
            }
            else
            {
                await using var stream = new FileStream(absPath, FileMode.Create);
                await file.CopyToAsync(stream);
                _log.LogInformation("Medlemslogotyp sparad: {FileName}", fn);
            }

            return $"/{relDir.Replace("\\", "/")}/{fn}";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid sparande av medlemslogotyp: {FileName}", file?.FileName);
            throw;
        }
    }

    // Raderar en medlemslogotyp.
    public Task DeleteMemberLogoAsync(string imageUrl)
    {
        try
        {
            var relPath = imageUrl.TrimStart('/');
            var absPath = Path.Combine(_env.WebRootPath, relPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (File.Exists(absPath))
            {
                File.Delete(absPath);
                _log.LogInformation("Medlemslogotyp raderad: {Path}", absPath);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fel vid radering av medlemslogotyp: {Path}", imageUrl);
        }
        return Task.CompletedTask;
    }
}