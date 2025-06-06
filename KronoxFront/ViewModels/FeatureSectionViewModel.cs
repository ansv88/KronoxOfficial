namespace KronoxFront.ViewModels;

// ViewModel f�r en feature-sektion p� en sida.
public class FeatureSectionViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string ImageAltText { get; set; } = "";
    public bool HasImage { get; set; }
    public int SortOrder { get; set; }
    public string PageKey { get; set; } = "";
}