using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request-modell f�r att koppla ett befintligt dokument till en nyhet, med valfritt visningsnamn och sorteringsordning.
public class AttachDocumentRequest
{
    [Required(ErrorMessage = "Dokument-id kr�vs.")]
    [Range(1, int.MaxValue, ErrorMessage = "Ogiltigt dokument-id.")]
    public int DocumentId { get; set; }

    [StringLength(200, ErrorMessage = "Visningsnamn f�r vara max 200 tecken.")]
    public string? DisplayName { get; set; }

    [Range(0, 999, ErrorMessage = "Sorteringsordning m�ste vara mellan 0 och 999.")]
    public int SortOrder { get; set; } = 0;
}