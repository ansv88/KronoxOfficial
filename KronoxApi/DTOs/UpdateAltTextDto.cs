using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

public class UpdateAltTextDto
{
    [Required(ErrorMessage = "Alt-text är obligatorisk.")]
    [StringLength(200, ErrorMessage = "Alt-text får vara max 200 tecken.")]
    public string AltText { get; set; } = string.Empty;
}