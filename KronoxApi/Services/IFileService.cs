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

    // Sida-bilder
    Task<string> SavePageImageAsync(IFormFile file, string pageKey, bool preserveFilename);
    Task DeletePageImageAsync(string imageUrl);

    // Feature-avsnitt
    Task<string> SaveFeatureImageAsync(IFormFile file, string indexIdentifier, bool preserveFilename);
    Task DeleteFeatureImageAsync(string imageUrl);

    // Medlemslogotyper
    Task<string> SaveMemberLogoAsync(IFormFile file);
    Task DeleteMemberLogoAsync(string imageUrl);
}