namespace KronoxFront.DTOs;

public class DocumentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long FileSize { get; set; }
    public int MainCategoryId { get; set; }
    public List<int> SubCategories { get; set; } = new();
    public MainCategoryDto MainCategory { get; set; } = new();
    public List<SubCategoryDto> SubCategoryDtos { get; set; } = new();

    // Fält för arkivering
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }
    public string? ArchivedBy { get; set; }

    // Hjälpmetod för formaterad filstorlek
    public string FormattedFileSize => FormatFileSize(FileSize);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        var order = 0;

        // Skala ned tills värdet är < 1024 eller vi nått högsta enhet
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024.0;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}