using System.ComponentModel.DataAnnotations;
using KronoxFront.Validators;

namespace KronoxFront.ViewModels;

public class CustomPageViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "PageKey är obligatoriskt")]
    [StringLength(100, ErrorMessage = "PageKey får vara max 100 tecken")]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "PageKey får endast innehålla små bokstäver, siffror och bindestreck")]
    [CustomValidation(typeof(PageKeyValidator), nameof(PageKeyValidator.ValidatePageKey))]
    public string PageKey { get; set; } = "";

    [Required(ErrorMessage = "Titel är obligatoriskt")]
    [StringLength(200, ErrorMessage = "Titel får vara max 200 tecken")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Visningsnamn är obligatoriskt")]
    [StringLength(100, ErrorMessage = "Visningsnamn får vara max 100 tecken")]
    public string DisplayName { get; set; } = "";

    [StringLength(500, ErrorMessage = "Beskrivning får vara max 500 tecken")]
    public string Description { get; set; } = "";

    public bool IsActive { get; set; } = true;
    public bool ShowInNavigation { get; set; } = true;

    [StringLength(20, ErrorMessage = "Navigationstyp får vara max 20 tecken")]
    public string NavigationType { get; set; } = "main";

    [StringLength(100, ErrorMessage = "Föräldrasida får vara max 100 tecken")]
    public string? ParentPageKey { get; set; }

    [Range(0, 99, ErrorMessage = "Sorteringsordning måste vara mellan 0 och 99")]
    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public string CreatedBy { get; set; } = "";
    public List<string> RequiredRoles { get; set; } = new();
    public List<SectionConfigItem> SectionConfig { get; set; } = new();
}