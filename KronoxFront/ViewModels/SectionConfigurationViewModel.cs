using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels;

// Enum f�r olika typer av sektioner som kan visas p� en sida
public enum SectionType
{
    [Display(Name = "Bannerbild")]
    Banner = 1,

    [Display(Name = "Intro-sektion")]
    Intro = 2,

    [Display(Name = "Navigeringsknappar")]
    NavigationButtons = 3,

    [Display(Name = "Feature-sektioner")]
    FeatureSections = 4,

    [Display(Name = "FAQ-sektioner")]
    FaqSections = 5,

    [Display(Name = "Dokumentsektion")]
    DocumentSection = 6,

    [Display(Name = "Medlemslogotyper")]
    MemberLogos = 7,

    [Display(Name = "Kontaktformul�r")]
    ContactForm = 8,

    [Display(Name = "Nyhetssektion")]
    NewsSection = 9,

    [Display(Name = "Handlingsplantabell")]
    ActionPlanTable = 10,

    [Display(Name = "Utvecklingsf�rslagformul�r")]
    DevelopmentSuggestionForm = 11
}