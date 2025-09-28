using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// För att inkludera bilddata vid behov
public class RegisterMemberLogoDto
{
    [Required(ErrorMessage = "Källsökväg krävs.")]
    [StringLength(500, ErrorMessage = "Källsökvägen är för lång.")]
    public string SourcePath { get; set; } = "";

    [Required(ErrorMessage = "Originalfilnamn krävs.")]
    [StringLength(255, ErrorMessage = "Filnamnet är för långt.")]
    public string OriginalFileName { get; set; } = "";

    [StringLength(200, ErrorMessage = "Alt-text får vara max 200 tecken.")]
    public string AltText { get; set; } = "";

    public int SortOrd { get; set; }

    [StringLength(2048, ErrorMessage = "Länken är för lång.")]
    [Url(ErrorMessage = "Ogiltig webbadress.")]
    public string LinkUrl { get; set; } = "";
}