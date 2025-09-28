using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request för att uppdatera ett dokuments kategorier.
public class UpdateDocumentRequest
{
    [Required(ErrorMessage = "Huvudkategori måste anges")]
    [Range(1, int.MaxValue, ErrorMessage = "Ogiltig huvudkategori.")]
    public int MainCategoryId { get; set; }

    // Gör listan icke-null för enklare hantering i controller
    public List<int> SubCategoryIds { get; set; } = new();
}