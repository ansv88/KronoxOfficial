namespace KronoxFront.DTOs;

// API response-modell för nyhetslista
public class NewsApiResponse
{
    public List<NewsApiItem> Posts { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

// API-modell för enskild nyhet
public class NewsApiItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public DateTime? ScheduledPublishDate { get; set; }
    public bool IsArchived { get; set; }
    public string VisibleToRoles { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime LastModified { get; set; }
}