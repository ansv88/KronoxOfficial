using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Representerar ett nyhetsinlägg i systemet
public class NewsModel
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;     // HTML-innehåll från TinyMCE
    
    public DateTime PublishedDate { get; set; }
    
    // Datum från vilket nyheten ska vara synlig (kan vara i framtiden)
    public DateTime? ScheduledPublishDate { get; set; }
    
    public bool IsArchived { get; set; }
    
    // Kommaseparerad sträng med roller som får se nyheten (t.ex. "Member,Admin,Styrelse")
    [StringLength(500)]
    public string VisibleToRoles { get; set; } = "Member"; // Standard är att alla medlemmar ser nyheten
    
    // Skapande-datum
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    // Senaste uppdaterings-datum  
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // Navigation property för kopplade dokument
    public virtual ICollection<NewsDocument> NewsDocuments { get; set; } = new List<NewsDocument>();
}