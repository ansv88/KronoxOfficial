namespace KronoxApi.Services;

/// <summary>
/// Tjänstegränssnitt för filhantering, validering och lagring.
/// </summary>
public interface IFileService
{
    // Validering
    string? ValidateImage(IFormFile file);
    string? ValidateDocument(IFormFile file);

    // Dokument
    Task<string> SaveDocumentAsync(IFormFile file, string category);
    Task DeleteDocumentAsync(string relativePath);
    string GetContentType(string fileName);

    // Medlemslogotyper
    Task<string> SaveMemberLogoAsync(IFormFile file);
    Task DeleteMemberLogoAsync(string imageUrl);
}