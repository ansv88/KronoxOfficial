using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request-modell f�r att uppdatera en befintlig nyhet.
public class UpdateNewsRequest
{
    [Required(ErrorMessage = "Titeln �r obligatorisk.")]
    [StringLength(200, ErrorMessage = "Titeln f�r inte �verstiga 200 tecken.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Inneh�llet �r obligatoriskt.")]
    public string Content { get; set; } = string.Empty;

    // Datum fr�n vilket nyheten ska vara synlig
    public DateTime? ScheduledPublishDate { get; set; }

    public bool IsArchived { get; set; }

    // Roller som f�r se nyheten (kommaseparerad str�ng)
    [StringLength(500)]
    public string VisibleToRoles { get; set; } = "Medlem";
}