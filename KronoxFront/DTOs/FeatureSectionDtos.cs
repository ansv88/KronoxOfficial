namespace KronoxFront.DTOs;

public class FeatureSectionDto
{
    public int Id { get; set; }
    public string PageKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ImageAltText { get; set; } = string.Empty;
    public bool HasImage { get; set; }
    public int SortOrder { get; set; }
    public bool HasPrivateContent { get; set; }
}

public class FeatureSectionWithPrivateDto : FeatureSectionDto
{
    public string PrivateContent { get; set; } = string.Empty;
    public string ContactPersonsJson { get; set; } = string.Empty;
    public string ContactHeading { get; set; } = string.Empty;

    // Deserialize ContactPersons för enklare användning
    public List<ContactPersonDto> ContactPersons { get; set; } = new();
}

public class ContactPersonDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
}