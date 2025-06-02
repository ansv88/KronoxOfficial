using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request för att uppdatera ett dokuments kategorier.
public class UpdateDocumentRequest
{
    [Required(ErrorMessage = "Huvudkategori måste anges")]
    public int MainCategoryId { get; set; }
    public List<int>? SubCategoryIds { get; set; } = new();
}