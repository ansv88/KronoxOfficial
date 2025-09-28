namespace KronoxApi.Models;

// Kopplingsmodell mellan nyheter och dokument (visningsnamn och sortering)
public class NewsDocument
{
    public int Id { get; set; }
    public int NewsId { get; set; }
    public int DocumentId { get; set; }
    public string? DisplayName { get; set; } // Alternativt visningsnamn
    public int SortOrder { get; set; } = 0;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual NewsModel News { get; set; } = null!;
    public virtual Document Document { get; set; } = null!;
}