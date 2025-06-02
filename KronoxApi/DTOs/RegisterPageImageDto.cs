using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// DTO för att registrera en befintlig bild som redan finns på servern. Används när en fil redan har laddats upp manuellt till servern.
public class RegisterPageImageDto
{
    [Required]
    public string SourcePath { get; set; } = string.Empty; // Sökväg till källbilden på servern (relativ från wwwroot)

    [Required]
    public string PageKey { get; set; } = string.Empty;

    [Required]
    public string AltText { get; set; } = string.Empty;

    public bool PreserveFilename { get; set; } = false;

}