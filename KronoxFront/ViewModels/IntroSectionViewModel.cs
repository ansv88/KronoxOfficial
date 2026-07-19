using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels;

// ViewModel för intro-sektionen på olika sidor, inklusive breadcrumb och navigeringsknappar.
public class IntroSectionViewModel
{
    [StringLength(100, ErrorMessage = "Rubriken får vara max 100 tecken.")]
    public string Title { get; set; } = "";

    public string Content { get; set; } = "";

    public string ImageUrl { get; set; } = "";

    [StringLength(200, ErrorMessage = "Bildbeskrivningen får vara max 200 tecken.")]
    public string ImageAltText { get; set; } = "";

    public bool HasImage { get; set; } = false;

    [StringLength(60, ErrorMessage = "Breadcrumb-titeln får vara max 60 tecken.")]
    public string BreadcrumbTitle { get; set; } = "";

    public bool ShowNavigationButtons { get; set; } = false;
    public List<NavigationButtonViewModel> NavigationButtons { get; set; } = new();
}