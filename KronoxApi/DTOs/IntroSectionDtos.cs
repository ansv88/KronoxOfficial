using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// DTO f�r intro-sektioner med breadcrumb och navigeringsknappar.
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

// DTO f�r navigeringsknappar.
public class NavigationButtonDto
{
    [Required, StringLength(100, ErrorMessage = "Text f�r vara max 100 tecken.")]
    public string Text { get; set; } = "";

    [Required, Url(ErrorMessage = "Ogiltig webbadress.")]
    [StringLength(2048, ErrorMessage = "URL �r f�r l�ng.")]
    public string Url { get; set; } = "";

    public string IconClass { get; set; } = "fa-solid fa-arrow-right";
    public int SortOrder { get; set; }
}