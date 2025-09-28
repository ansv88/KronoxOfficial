using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels.MemberLogos;

public class EditLogoViewModel
{
    [StringLength(200, ErrorMessage = "Alt-text f�r vara max 200 tecken.")]
    public string AltText { get; set; } = "";

    [StringLength(2048, ErrorMessage = "L�nken �r f�r l�ng.")]
    [Url(ErrorMessage = "Ogiltig webbadress.")]
    public string LinkUrl { get; set; } = "";
}