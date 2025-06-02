namespace KronoxApi.Utilities;

// Hjälpklass för validering av filer och filnamn.
public static class FileValidation
{
    // Lista över tillåtna bildformat (samma som i FileService)
    public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };

    // Max bildstorlek (samma som i FileService)
    public const long MaxImageSize = 10 * 1024 * 1024;  // 10 MB

    // Lista över tillåtna dokumentformat (samma som i FileService)
    public static readonly string[] AllowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt" };

    // Max dokumentstorlek (samma som i FileService)
    public const long MaxDocumentSize = 25 * 1024 * 1024;  // 25 MB

    // Validerar en bildfil mot tillåtna format och maxstorlek
    public static bool ValidateImageFile(IFormFile file, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (file == null || file.Length == 0)
        {
            errorMessage = "Ingen fil har skickats.";
            return false;
        }

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        // Kontrollera filtillägget
        if (!AllowedImageExtensions.Contains(extension))
        {
            errorMessage = $"Ogiltigt filformat. Tillåtna format är: {string.Join(", ", AllowedImageExtensions)}";
            return false;
        }

        // Kontrollera filstorlek
        if (file.Length > MaxImageSize)
        {
            errorMessage = $"Filen är för stor. Maximal storlek är {MaxImageSize / (1024 * 1024)}MB.";
            return false;
        }

        return true;
    }

    // Kontrollerar om ett filnamn redan finns i målkatalogen
    public static bool FileExists(string directory, string fileName, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
        {
            errorMessage = "Ogiltig katalog eller filnamn.";
            return false;
        }

        // Använd originalfilnamnet utan modifiering
        var filePath = Path.Combine(directory, fileName);

        if (File.Exists(filePath))
        {
            errorMessage = $"En fil med namnet {fileName} finns redan.";
            return true;
        }

        return false;
    }
}