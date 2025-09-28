using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

public class UpdateAltTextDto
{
    [Required(ErrorMessage = "Alt-text �r obligatorisk.")]
    [StringLength(200, ErrorMessage = "Alt-text f�r vara max 200 tecken.")]
    public string AltText { get; set; } = string.Empty;
}