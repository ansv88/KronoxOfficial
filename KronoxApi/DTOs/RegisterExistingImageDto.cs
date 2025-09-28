using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

public class RegisterExistingImageDto
{
    // Relativ sökväg från wwwroot, t.ex. "images/pages/home/foo.jpg"
    [Required(ErrorMessage = "Källsökväg krävs.")]
    [StringLength(500, ErrorMessage = "Källsökvägen är för lång.")]
    public string SourcePath { get; set; } = "";

    // AltText används även för hero:...-märkning
    [Required(ErrorMessage = "Alt-text krävs.")]
    [StringLength(200, ErrorMessage = "Alt-text får vara max 200 tecken.")]
    public string AltText { get; set; } = "";
}