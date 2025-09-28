using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// DTO för att uppdatera webbadress (länk) för en medlemslogotyp.
public class LinkUrlUpdateDto
{
    [Required(ErrorMessage = "Länk krävs.")]
    [Url(ErrorMessage = "Ogiltig webbadress.")]
    [StringLength(2048, ErrorMessage = "Länken är för lång.")]
    public string LinkUrl { get; set; } = string.Empty;
}