using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Representerar en enskild fråga och svar inom en FAQ-sektion
public class FaqItem
{
    public int Id { get; set; }

    public int FaqSectionId { get; set; }

    [Required]
    public string Question { get; set; } = string.Empty;

    [Required] 
    public string Answer { get; set; } = string.Empty;
    
    public string ImageUrl { get; set; } = string.Empty;
    public string ImageAltText { get; set; } = string.Empty;
    public bool HasImage { get; set; } = false;
    
    public int SortOrder { get; set; }

    // Navigation property
    public FaqSection FaqSection { get; set; } = null!;
}