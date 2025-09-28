using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// F�r att inkludera bilddata vid behov
public class RegisterMemberLogoDto
{
    [Required(ErrorMessage = "K�lls�kv�g kr�vs.")]
    [StringLength(500, ErrorMessage = "K�lls�kv�gen �r f�r l�ng.")]
    public string SourcePath { get; set; } = "";

    [Required(ErrorMessage = "Originalfilnamn kr�vs.")]
    [StringLength(255, ErrorMessage = "Filnamnet �r f�r l�ngt.")]
    public string OriginalFileName { get; set; } = "";

    [StringLength(200, ErrorMessage = "Alt-text f�r vara max 200 tecken.")]
    public string AltText { get; set; } = "";

    public int SortOrd { get; set; }

    [StringLength(2048, ErrorMessage = "L�nken �r f�r l�ng.")]
    [Url(ErrorMessage = "Ogiltig webbadress.")]
    public string LinkUrl { get; set; } = "";
}