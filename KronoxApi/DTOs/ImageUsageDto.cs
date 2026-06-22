namespace KronoxApi.DTOs;

// DTO som beskriver var en bild används (renderingskälla + sida) för bildbibliotekets översikt och delete-guard.
public class ImageUsageDto
{
    public string Url { get; set; } = string.Empty;
    public string PageKey { get; set; } = string.Empty;
    public string UsageType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}