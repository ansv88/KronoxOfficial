namespace KronoxApi.Models;

// Representerar ett dokument som laddats upp till systemet.
public class Document
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long FileSize { get; set; }
    public int MainCategoryId { get; set; }
    public List<int> SubCategories { get; set; } = new();
}