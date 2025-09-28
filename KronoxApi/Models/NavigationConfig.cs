using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Navigationspost (visningsnamn, typ, synlighet för gäst/medlem, roller, sortering).
public class NavigationConfig
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string PageKey { get; set; } = "";
    
    [Required] 
    [StringLength(200)]
    public string DisplayName { get; set; } = "";
    
    [Required]
    [StringLength(20)] 
    public string ItemType { get; set; } = "";
    
    public int SortOrder { get; set; }
    public int? GuestSortOrder { get; set; }
    public int? MemberSortOrder { get; set; } 
    
    public bool IsVisibleToGuests { get; set; } = true;
    public bool IsVisibleToMembers { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool IsSystemItem { get; set; } = false;
    
    [StringLength(500)]
    public string? RequiredRoles { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}