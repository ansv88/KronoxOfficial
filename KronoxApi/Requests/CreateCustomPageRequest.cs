using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request-modell f�r att skapa en anpassad sida (CustomPage) med navigation, sortering och rollkrav.
public class CreateCustomPageRequest
{
    [Required(ErrorMessage = "PageKey �r obligatoriskt")]
    [StringLength(100, ErrorMessage = "PageKey f�r vara max 100 tecken")]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "PageKey f�r endast inneh�lla sm� bokst�ver, siffror och bindestreck")]
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
    [RegularExpression("^(main|dropdown|hidden)$", ErrorMessage = "Navigationstyp m�ste vara en av: main, dropdown, hidden.")]
    public string NavigationType { get; set; } = "main";

    [StringLength(100, ErrorMessage = "F�r�ldrasida f�r vara max 100 tecken")]
    public string? ParentPageKey { get; set; }

    [Range(0, 100, ErrorMessage = "Sorteringsordning m�ste vara mellan 0 och 100")]
    public int SortOrder { get; set; } = 0;

    public List<string> RequiredRoles { get; set; } = new();
}