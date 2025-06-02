namespace KronoxApi.DTOs;

// DTO för att representera ett innehållsblock och dess bilder.
public class ContentBlockDto
{
    public string PageKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string Metadata { get; set; } = "{}";
    public DateTime LastModified { get; set; }
    public List<PageImageDto> Images { get; set; } = new();
}