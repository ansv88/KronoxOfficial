using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

public class NavigationConfigDto
{
    public int Id { get; set; }
    public string PageKey { get; set; } = "";

    [Required(ErrorMessage = "Länktext krävs.")]
    [StringLength(40, ErrorMessage = "Länktexten får vara max 40 tecken.")]
    public string DisplayName { get; set; } = "";

    public string ItemType { get; set; } = "";
    public int SortOrder { get; set; }
    public int? GuestSortOrder { get; set; }
    public int? MemberSortOrder { get; set; }
    public bool IsVisibleToGuests { get; set; } = true;
    public bool IsVisibleToMembers { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool IsSystemItem { get; set; } = false;
    public string? RequiredRoles { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
}

public class NavigationUpdateDto
{
    [Required(ErrorMessage = "Länktext krävs.")]
    [StringLength(40, ErrorMessage = "Länktexten får vara max 40 tecken.")]
    public string DisplayName { get; set; } = "";

    public int SortOrder { get; set; }
    public int? GuestSortOrder { get; set; }
    public int? MemberSortOrder { get; set; }

    public bool IsVisibleToGuests { get; set; }
    public bool IsVisibleToMembers { get; set; }
    public bool IsActive { get; set; }

    [StringLength(500, ErrorMessage = "För många eller för långa roller.")]
    public string? RequiredRoles { get; set; }
}