using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

public class FeatureSectionDto
{
    public int Id { get; set; }

    public string PageKey { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Titel f�r vara max 200 tecken.")]
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Bild-URL f�r vara max 500 tecken.")]
    public string ImageUrl { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Alt-text f�r vara max 200 tecken.")]
    public string ImageAltText { get; set; } = string.Empty;

    public bool HasImage { get; set; }
    public int SortOrder { get; set; }
    public bool HasPrivateContent { get; set; }

    // Hj�lpkonstruktor f�r enkel mappning fr�n Entity
    public FeatureSectionDto() { }
    
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
        HasPrivateContent = f.HasPrivateContent;
    }
}

public class FeatureSectionWithPrivateDto : FeatureSectionDto
{
    public string PrivateContent { get; set; } = string.Empty;
    public string ContactPersonsJson { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Kontakt-rubrik f�r vara max 200 tecken.")]
    public string ContactHeading { get; set; } = string.Empty;

    // Deserialize ContactPersons f�r enklare anv�ndning
    public List<ContactPersonDto> ContactPersons { get; set; } = new();
}

public class ContactPersonDto
{
    [StringLength(100, ErrorMessage = "Namn f�r vara max 100 tecken.")]
    public string Name { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Ogiltig e-postadress.")]
    [StringLength(100, ErrorMessage = "E-post f�r vara max 100 tecken.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Telefon f�r vara max 50 tecken.")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Organisation f�r vara max 100 tecken.")]
    public string Organization { get; set; } = string.Empty;
}