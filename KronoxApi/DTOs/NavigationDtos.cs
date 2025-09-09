namespace KronoxApi.DTOs;

public class NavigationConfigDto
{
    public int Id { get; set; }
    public string PageKey { get; set; } = "";
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
    public string DisplayName { get; set; } = "";
    public int SortOrder { get; set; }
    public int? GuestSortOrder { get; set; }
    public int? MemberSortOrder { get; set; }
    public bool IsVisibleToGuests { get; set; }
    public bool IsVisibleToMembers { get; set; }
    public bool IsActive { get; set; }
    public string? RequiredRoles { get; set; }
}