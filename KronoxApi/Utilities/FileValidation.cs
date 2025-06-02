namespace KronoxApi.Utilities;

// Hj�lpklass f�r validering av filer och filnamn.
public static class FileValidation
{
    // Lista �ver till�tna bildformat (samma som i FileService)
    public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };

    // Max bildstorlek (samma som i FileService)
    public const long MaxImageSize = 10 * 1024 * 1024;  // 10 MB

    // Lista �ver till�tna dokumentformat (samma som i FileService)
    public static readonly string[] AllowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt" };

    // Max dokumentstorlek (samma som i FileService)
    public const long MaxDocumentSize = 25 * 1024 * 1024;  // 25 MB

    // Validerar en bildfil mot till�tna format och maxstorlek
    public static bool ValidateImageFile(IFormFile file, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (file == null || file.Length == 0)
        {
            errorMessage = "Ingen fil har skickats.";
            return false;
        }

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        // Kontrollera filtill�gget
        if (!AllowedImageExtensions.Contains(extension))
        {
            errorMessage = $"Ogiltigt filformat. Till�tna format �r: {string.Join(", ", AllowedImageExtensions)}";
            return false;
        }

        // Kontrollera filstorlek
        if (file.Length > MaxImageSize)
        {
            errorMessage = $"Filen �r f�r stor. Maximal storlek �r {MaxImageSize / (1024 * 1024)}MB.";
            return false;
        }

        return true;
    }

    // Kontrollerar om ett filnamn redan finns i m�lkatalogen
    public static bool FileExists(string directory, string fileName, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
        {
            errorMessage = "Ogiltig katalog eller filnamn.";
            return false;
        }

        // Anv�nd originalfilnamnet utan modifiering
        var filePath = Path.Combine(directory, fileName);

        if (File.Exists(filePath))
        {
            errorMessage = $"En fil med namnet {fileName} finns redan.";
            return true;
        }

        return false;
    }
}