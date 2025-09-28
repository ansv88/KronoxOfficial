using KronoxFront.DTOs;

namespace KronoxFront.ViewModels;

// ViewModel för dokument med UI-specifika hjälpmetoder
public class DocumentViewModel
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long FileSize { get; set; }
    public int MainCategoryId { get; set; }
    public List<int>? SubCategories { get; set; } = new();
    public MainCategoryViewModel? MainCategory { get; set; }
    public MainCategoryDto MainCategoryDto { get; set; } = new();
    public List<SubCategoryDto>? SubCategoryDtos { get; set; } = new();

    // Fält för arkivering
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }
    public string? ArchivedBy { get; set; }

    // UI-specifika hjälpmetoder
    public string FormattedFileSize => FormatFileSize(FileSize);
    public string FileExtension => Path.GetExtension(FileName).ToLowerInvariant();
    public string FileIconClass => GetFileIconClass();

    // Säkerhetshjälpmetoder för UI
    public bool IsAccessibleByRole(string role)
    {
        if (!MainCategoryDto.AllowedRoles.Any()) return true; // Öppen för alla
        return MainCategoryDto.AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsAccessibleByRoles(IEnumerable<string> roles)
    {
        if (!MainCategoryDto.AllowedRoles.Any()) return true; // Öppen för alla
        return MainCategoryDto.AllowedRoles.Any(allowedRole =>
            roles.Contains(allowedRole, StringComparer.OrdinalIgnoreCase));
    }

    public string GetAccessibilityDescription()
    {
        if (!MainCategoryDto.AllowedRoles.Any())
            return "Alla användare";
        return $"Begränsad till: {string.Join(", ", MainCategoryDto.AllowedRoles)}";
    }

    private string GetFileIconClass()
    {
        return FileExtension switch
        {
            ".pdf" => "fa-solid fa-file-pdf text-danger",
            ".doc" or ".docx" => "fa-solid fa-file-word text-primary",
            ".xls" or ".xlsx" => "fa-solid fa-file-excel text-success",
            ".ppt" or ".pptx" => "fa-solid fa-file-powerpoint text-warning",
            ".txt" => "fa-solid fa-file-lines text-secondary",
            ".zip" or ".rar" or ".7z" => "fa-solid fa-file-zipper text-info",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "fa-solid fa-file-image text-purple",
            _ => "fa-solid fa-file text-muted"
        };
    }

    private static string FormatFileSize(long bytes)
    {
        const int scale = 1024;
        string[] orders = { "GB", "MB", "KB", "Bytes" };
        long max = (long)Math.Pow(scale, orders.Length - 1);

        foreach (string order in orders)
        {
            if (bytes > max)
                return $"{decimal.Divide(bytes, max):##.##} {order}";
            max /= scale;
        }
        return "0 Bytes";
    }
}