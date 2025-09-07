using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

public class CustomPage
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string PageKey { get; set; } = "";

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = "";

    [StringLength(500)]
    public string Description { get; set; } = "";

    public bool IsActive { get; set; } = true;
    public bool ShowInNavigation { get; set; } = true;

    [StringLength(20)]
    public string NavigationType { get; set; } = "main"; // "main", "dropdown", "footer", "hidden"

    [StringLength(100)]
    public string? ParentPageKey { get; set; }

    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string CreatedBy { get; set; } = "";

    // Kommer att konverteras till kommaseparerad sträng i databasen
    public List<string> RequiredRoles { get; set; } = new();
}