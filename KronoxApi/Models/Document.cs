using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Representerar ett dokument som laddats upp till systemet (filnamn, storlek, kategori/underkategorier, arkivering).
public class Document
{
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }
    public long FileSize { get; set; }
    public int MainCategoryId { get; set; }
    public List<int> SubCategories { get; set; } = new();

    // Fält för arkivering
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }
    public string? ArchivedBy { get; set; }

    // Navigation property
    public MainCategory? MainCategory { get; set; }
}