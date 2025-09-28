using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// För att ladda upp en ny logotyp
public class MemberLogoUploadDto
{
    [Required(ErrorMessage = "Fil krävs.")]
    public IFormFile File { get; set; } = null!;

    [StringLength(200, ErrorMessage = "Alt-text får vara max 200 tecken.")]
    public string AltText { get; set; } = "";

    public int SortOrd { get; set; }

    [StringLength(2048, ErrorMessage = "Länken är för lång.")]
    [Url(ErrorMessage = "Ogiltig webbadress.")]
    public string LinkUrl { get; set; } = "";
}