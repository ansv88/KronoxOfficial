namespace KronoxFront.ViewModels;

public class FaqSectionViewModel
{
    public int Id { get; set; }
    public string PageKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<FaqItemViewModel> FaqItems { get; set; } = new();
}

public class FaqItemViewModel
{
    public int Id { get; set; }
    public int FaqSectionId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ImageAltText { get; set; } = string.Empty;
    public bool HasImage { get; set; } = false;
    public int SortOrder { get; set; }
}