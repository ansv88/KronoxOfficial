namespace KronoxApi.Models;

// Representerar ett nyhetsinlägg i systemet
public class NewsModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;     // HTML-innehåll från TinyMCE
    public DateTime PublishedDate { get; set; }
    public bool IsArchived { get; set; }
}