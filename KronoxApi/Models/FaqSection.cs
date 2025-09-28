using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Representerar en FAQ-sektion som kan innehålla flera frågor och svar
// En sektion per sida med beskrivning och sorteringsordning.
public class FaqSection
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string PageKey { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    // Navigation property
    public List<FaqItem> FaqItems { get; set; } = new();
}