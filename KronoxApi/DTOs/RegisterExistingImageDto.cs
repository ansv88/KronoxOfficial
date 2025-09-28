using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

public class RegisterExistingImageDto
{
    // Relativ s�kv�g fr�n wwwroot, t.ex. "images/pages/home/foo.jpg"
    [Required(ErrorMessage = "K�lls�kv�g kr�vs.")]
    [StringLength(500, ErrorMessage = "K�lls�kv�gen �r f�r l�ng.")]
    public string SourcePath { get; set; } = "";

    // AltText anv�nds �ven f�r hero:...-m�rkning
    [Required(ErrorMessage = "Alt-text kr�vs.")]
    [StringLength(200, ErrorMessage = "Alt-text f�r vara max 200 tecken.")]
    public string AltText { get; set; } = "";
}