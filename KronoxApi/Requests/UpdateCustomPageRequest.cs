using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

public class UpdateCustomPageRequest
{
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

    [Range(0, 100, ErrorMessage = "Sorteringsordning m�ste vara mellan 0 och 100")]
    public int SortOrder { get; set; } = 0;

    public List<string> RequiredRoles { get; set; } = new();
}