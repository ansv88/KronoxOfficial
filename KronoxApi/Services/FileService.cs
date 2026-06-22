using KronoxApi.Utilities;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace KronoxApi.Services;

/// <summary>
/// Tjänst för filhantering, validering och lagring. Inkluderar sanering av filnamn
/// och skydd mot path traversal vid läsning/skrivning.
/// </summary>
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

    // ------------- Validering ------------

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

    // ------------- Dokument -------------

    // Sparar ett dokument i angiven kategori. Returnerar relativ sökväg.
    public async Task<string> SaveDocumentAsync(IFormFile file, string category)
    {
        try
        {
            var safeCategory = SanitizeSegment(category);
            var relDir = Path.Combine("documents", safeCategory);
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
            var baseDir = Path.Combine(_env.ContentRootPath, PrivateRoot);
            if (!TryResolveUnderBase(baseDir, relativePath, out var absPath))
            {
                _log.LogWarning("Försök att radera dokument utanför tillåten katalog: {Path}", relativePath);
                return Task.CompletedTask;
            }

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

    // ------------- Medlemslogotyper -------------

    // Sparar en medlemslogotyp med innehållsbaserat (hashat) filnamn för säker cache-busting.
    public async Task<string> SaveMemberLogoAsync(IFormFile file)
    {
        try
        {
            var relDir = Path.Combine("images", "members");
            var absDir = Path.Combine(_env.WebRootPath, relDir);
            Directory.CreateDirectory(absDir);

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".img";

            // Hasha innehållet → ändrad bild ger nytt filnamn/URL → frontend hämtar färsk kopia.
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            var hash8 = ShortHash8(ms);
            ms.Position = 0;

            var baseName = SanitizeSegment(Path.GetFileNameWithoutExtension(file.FileName));
            baseName = StripExistingHashSuffix(baseName); // undvik dubbelhash vid återuppladdning

            var fn = $"{baseName}-{hash8}{ext}".ToLowerInvariant();
            var absPath = Path.Combine(absDir, fn);

            // Samma innehåll = samma filnamn → återanvänd befintlig fil.
            if (File.Exists(absPath))
            {
                _log.LogInformation("Medlemslogotyp med samma innehåll finns redan: {FileName}", fn);
            }
            else
            {
                await using var stream = new FileStream(absPath, FileMode.Create);
                await ms.CopyToAsync(stream);
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
            var baseDir = _env.WebRootPath;
            if (!TryResolveUnderBase(baseDir, imageUrl, out var absPath))
            {
                _log.LogWarning("Försök att radera medlemslogotyp utanför wwwroot: {Url}", imageUrl);
                return Task.CompletedTask;
            }

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

    // ------------- Hjälpmetoder -------------

    private static string SanitizeSegment(string input)
    {
        // Tillåt a-z, 0-9, bindestreck och underscore. Ersätt övrigt med bindestreck.
        if (string.IsNullOrWhiteSpace(input)) return "default";
        var normalized = input.Trim().ToLowerInvariant();
        return Regex.Replace(normalized, @"[^a-z0-9_-]", "-");
    }

    // SHA-256 → 8 tecken kort hash-suffix (samma princip som ContentController).
    private static string ShortHash8(Stream s)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(s);
        return Convert.ToHexString(hash).ToLowerInvariant()[..8];
    }

    // Tar bort ett ev. tidigare hash-suffix (-[0-9a-f]{8}) i slutet av basnamnet.
    private static string StripExistingHashSuffix(string baseName)
        => Regex.Replace(baseName, "-[0-9a-f]{8}$", "", RegexOptions.IgnoreCase);

    private static bool TryResolveUnderBase(string baseDir, string relativeOrUrl, out string absPath)
    {
        var relPath = relativeOrUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
        var candidate = Path.GetFullPath(Path.Combine(baseDir, relPath));
        var baseFull = Path.GetFullPath(baseDir);
        var isUnder = candidate.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase);
        absPath = isUnder ? candidate : string.Empty;
        return isUnder;
    }
}