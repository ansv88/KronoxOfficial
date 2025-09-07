namespace KronoxApi.DTOs;

public class CustomPageDto
{
    public int Id { get; set; }
    public string PageKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; }
    public bool ShowInNavigation { get; set; }
    public string NavigationType { get; set; } = "";
    public string? ParentPageKey { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public string CreatedBy { get; set; } = "";
    public List<string> RequiredRoles { get; set; } = new();
}

public class NavigationPageDto
{
    public string PageKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string NavigationType { get; set; } = "";
    public string? ParentPageKey { get; set; }
    public int SortOrder { get; set; }
    public List<string> RequiredRoles { get; set; } = new();
    public List<NavigationPageDto> Children { get; set; } = new();
}