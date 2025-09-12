namespace KronoxFront.ViewModels;

public class NavigationPageViewModel
{
    public string PageKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string NavigationType { get; set; } = "";
    public string? ParentPageKey { get; set; }
    public int SortOrder { get; set; }
    public List<string> RequiredRoles { get; set; } = new();
    public List<NavigationPageViewModel> Children { get; set; } = new();
}

public class NavigationItemViewModel
{
    public string Type { get; set; } = ""; // "static" eller "custom"
    public string PageKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int SortOrder { get; set; }
    public NavigationPageViewModel? CustomPage { get; set; }
}

public class NavigationConfigViewModel
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
}