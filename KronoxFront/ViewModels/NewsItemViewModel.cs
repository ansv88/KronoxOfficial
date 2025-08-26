using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels;

public class NewsItemViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Titel �r obligatorisk")]
    [StringLength(200, ErrorMessage = "Titeln f�r inte �verstiga 200 tecken")]
    public string Title { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Inneh�ll �r obligatoriskt")]
    public string Content { get; set; } = string.Empty;
    
    public DateTime PublishedDate { get; set; }
    
    public DateTime? ScheduledPublishDate { get; set; }
    
    public bool IsArchived { get; set; }
    
    public string VisibleToRoles { get; set; } = "Medlem";
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime LastModified { get; set; }
    
    
    // Helper properties f�r UI
    public bool IsScheduled => ScheduledPublishDate.HasValue && ScheduledPublishDate > DateTime.Now;
    public string FormattedPublishDate => FormatSwedishDateTime(PublishedDate);
    public string FormattedCreatedDate => FormatSwedishDateTime(CreatedDate);
    public List<string> RolesList => VisibleToRoles.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
    
    private string FormatSwedishDateTime(DateTime dateTime)
    {
        // Konvertera till svensk tid
        var swedishTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, 
            TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
        return swedishTime.ToString("yyyy-MM-dd HH:mm");
    }
}

public class NewsListViewModel
{
    public List<NewsItemViewModel> Posts { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public class CreateNewsViewModel
{
    [Required(ErrorMessage = "Titel �r obligatorisk")]
    [StringLength(200, ErrorMessage = "Titeln f�r inte �verstiga 200 tecken")]
    public string Title { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Inneh�ll �r obligatoriskt")]
    public string Content { get; set; } = string.Empty;
    
    public DateTime? ScheduledPublishDate { get; set; }
    
    public bool IsArchived { get; set; }
    
    public List<string> SelectedRoles { get; set; } = new() { "Member" };
    
    
    // Available roles for selection
    public List<RoleOption> AvailableRoles { get; set; } = new()
    {
        new RoleOption { Value = "Alla", Label = "Alla (alla anv�ndare)", IsSelected = true },
        new RoleOption { Value = "Admin", Label = "Administrat�rer", IsSelected = false },
        new RoleOption { Value = "Styrelse", Label = "Styrelsen", IsSelected = false },
        new RoleOption { Value = "Medlem", Label = "Medlemmar", IsSelected = false }
    };
}

public class RoleOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}