using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// DTO f�r att registrera en befintlig bild som redan finns p� servern. Anv�nds n�r en fil redan har laddats upp manuellt till servern.
public class RegisterPageImageDto
{
    [Required]
    public string SourcePath { get; set; } = string.Empty; // S�kv�g till k�llbilden p� servern (relativ fr�n wwwroot)

    [Required]
    public string PageKey { get; set; } = string.Empty;

    [Required]
    public string AltText { get; set; } = string.Empty;

    public bool PreserveFilename { get; set; } = false;

}