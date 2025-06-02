using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// DTO för att representera en feature-sektion på en sida, inklusive eventuell bild.
public class FeatureSectionDto
{
    public int Id { get; set; }

    public string PageKey { get; set; } = "";

    public string Title { get; set; } = "";

    public string Content { get; set; } = "";

    public string ImageUrl { get; set; } = "";

    public string ImageAltText { get; set; } = "";

    public bool HasImage { get; set; }

    public int SortOrder { get; set; }

    // Parameterlös konstruktor krävs av deserialiserare
    public FeatureSectionDto() { }

    // Hjälpkonstruktor för enkel mappning från din Entity
    public FeatureSectionDto(Models.FeatureSection f)
    {
        Id = f.Id;
        PageKey = f.PageKey;
        Title = f.Title;
        Content = f.Content;
        ImageUrl = f.ImageUrl;
        ImageAltText = f.ImageAltText;
        HasImage = f.HasImage;
        SortOrder = f.SortOrder;
    }
}
