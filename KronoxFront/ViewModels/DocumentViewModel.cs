using KronoxFront.DTOs;

namespace KronoxFront.ViewModels;

public class DocumentViewModel
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long FileSize { get; set; }
    public int MainCategoryId { get; set; }
    public List<int>? SubCategories { get; set; } = new();
    public MainCategoryDto MainCategoryDto { get; set; } = new();
    public List<SubCategoryDto>? SubCategoryDtos { get; set; } = new();
}