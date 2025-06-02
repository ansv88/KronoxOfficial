using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Representerar ett innehållsblock (t.ex. en sektion på en sida).
public class ContentBlock
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string PageKey { get; set; } = "";

    [Required, MaxLength(255)]
    public string Title { get; set; } = "";

    public string HtmlContent { get; set; } = "";

    public string Metadata { get; set; } = "{}";

    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // Navigationsproperty för bilder kopplade till blocket
    public ICollection<PageImage> PageImages { get; set; } = new List<PageImage>();
}