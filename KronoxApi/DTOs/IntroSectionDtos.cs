namespace KronoxApi.DTOs;

// DTO för intro-sektioner med breadcrumb och navigeringsknappar.
public class IntroSectionDto
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string ImageAltText { get; set; } = "";
    public bool HasImage { get; set; } = false;
    public string BreadcrumbTitle { get; set; } = "";
    public bool ShowNavigationButtons { get; set; } = false;
    public List<NavigationButtonDto> NavigationButtons { get; set; } = new();
}

// DTO för navigeringsknappar.
public class NavigationButtonDto
{
    public string Text { get; set; } = "";
    public string Url { get; set; } = "";
    public string IconClass { get; set; } = "fa-solid fa-arrow-right";
    public int SortOrder { get; set; }
}