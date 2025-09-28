using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

public class FaqSectionDto
{
    public int Id { get; set; }
    public string PageKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<FaqItemDto> FaqItems { get; set; } = new();
}

public class FaqItemDto
{
    public int Id { get; set; }
    public int FaqSectionId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ImageAltText { get; set; } = string.Empty;
    public bool HasImage { get; set; } = false;
    public int SortOrder { get; set; }
}

public class CreateFaqSectionDto
{
    [Required, StringLength(50, ErrorMessage = "PageKey f�r vara max 50 tecken.")]
    public string PageKey { get; set; } = string.Empty;

    [Required, StringLength(200, ErrorMessage = "Titel f�r vara max 200 tecken.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Beskrivning f�r vara max 1000 tecken.")]
    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

public class CreateFaqItemDto
{
    [Required]
    public int FaqSectionId { get; set; }

    [Required, StringLength(500, ErrorMessage = "Fr�gan f�r vara max 500 tecken.")]
    public string Question { get; set; } = string.Empty;

    [Required(ErrorMessage = "Svar kr�vs.")]
    public string Answer { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Bild-URL f�r vara max 500 tecken.")]
    public string ImageUrl { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Alt-text f�r vara max 200 tecken.")]
    public string ImageAltText { get; set; } = string.Empty;

    public bool HasImage { get; set; } = false;
    public int SortOrder { get; set; }
}