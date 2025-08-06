namespace KronoxFront.ViewModels;

// ViewModel för en feature-sektion på en sida.
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
    public bool HasPrivateContent { get; set; } = false;
    public string PrivateContent { get; set; } = "";
    public string ContactHeading { get; set; } = "";
    public List<ContactPersonViewModel> ContactPersons { get; set; } = new();
}