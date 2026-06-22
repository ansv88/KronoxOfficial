namespace KronoxFront.DTOs;

// Speglar API:ts ImageUsageDto – beskriver var en bild används (för bildbibliotekets översikt).
public class ImageUsageDto
{
    public string Url { get; set; } = "";
    public string PageKey { get; set; } = "";
    public string UsageType { get; set; } = "";
    public string Description { get; set; } = "";
}