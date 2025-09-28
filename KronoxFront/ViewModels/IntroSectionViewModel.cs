namespace KronoxFront.ViewModels;

// ViewModel f�r intro-sektionen p� olika sidor, inklusive breadcrumb och navigeringsknappar.
public class IntroSectionViewModel
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string ImageAltText { get; set; } = "";
    public bool HasImage { get; set; } = false;

    // Nya f�lt f�r breadcrumb och navigation
    public string BreadcrumbTitle { get; set; } = "";
    public bool ShowNavigationButtons { get; set; } = false;
    public List<NavigationButtonViewModel> NavigationButtons { get; set; } = new();
}