using System.ComponentModel.DataAnnotations;
using KronoxFront.Validators;

namespace KronoxFront.ViewModels;

public class CustomPageViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "PageKey �r obligatoriskt")]
    [StringLength(100, ErrorMessage = "PageKey f�r vara max 100 tecken")]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "PageKey f�r endast inneh�lla sm� bokst�ver, siffror och bindestreck")]
    [CustomValidation(typeof(PageKeyValidator), nameof(PageKeyValidator.ValidatePageKey))]
    public string PageKey { get; set; } = "";

    [Required(ErrorMessage = "Titel �r obligatoriskt")]
    [StringLength(200, ErrorMessage = "Titel f�r vara max 200 tecken")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Visningsnamn �r obligatoriskt")]
    [StringLength(100, ErrorMessage = "Visningsnamn f�r vara max 100 tecken")]
    public string DisplayName { get; set; } = "";

    [StringLength(500, ErrorMessage = "Beskrivning f�r vara max 500 tecken")]
    public string Description { get; set; } = "";

    public bool IsActive { get; set; } = true;
    public bool ShowInNavigation { get; set; } = true;

    [StringLength(20, ErrorMessage = "Navigationstyp f�r vara max 20 tecken")]
    public string NavigationType { get; set; } = "main";

    [StringLength(100, ErrorMessage = "F�r�ldrasida f�r vara max 100 tecken")]
    public string? ParentPageKey { get; set; }

    [Range(0, 99, ErrorMessage = "Sorteringsordning m�ste vara mellan 0 och 99")]
    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public string CreatedBy { get; set; } = "";
    public List<string> RequiredRoles { get; set; } = new();
    public List<SectionConfigItem> SectionConfig { get; set; } = new();
}