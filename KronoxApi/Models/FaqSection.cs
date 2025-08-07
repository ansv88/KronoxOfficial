using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Representerar en FAQ-sektion som kan innehålla flera frågor och svar
public class FaqSection
{
    public int Id { get; set; }

    [Required]
    public string PageKey { get; set; } = string.Empty;

    [Required] 
    public string Title { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public int SortOrder { get; set; }
    
    // Navigation property
    public List<FaqItem> FaqItems { get; set; } = new();
}