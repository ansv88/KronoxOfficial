using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request-modell för att uppdatera en befintlig nyhet.
public class UpdateNewsRequest
{
    [Required(ErrorMessage = "Titeln är obligatorisk.")]
    [StringLength(200, ErrorMessage = "Titeln får inte överstiga 200 tecken.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Innehållet är obligatoriskt.")]
    public string Content { get; set; } = string.Empty;

    // Datum från vilket nyheten ska vara synlig
    public DateTime? ScheduledPublishDate { get; set; }

    public bool IsArchived { get; set; }

    // Roller som får se nyheten (kommaseparerad sträng)
    [StringLength(500)]
    public string VisibleToRoles { get; set; } = "Medlem";
}