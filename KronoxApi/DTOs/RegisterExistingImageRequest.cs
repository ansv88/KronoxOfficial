namespace KronoxApi.DTOs;

public class RegisterExistingImageRequest
{
    // Relativ s�kv�g fr�n wwwroot, t.ex. "images/pages/home/foo.jpg"
    public string SourcePath { get; set; } = "";
    // AltText anv�nds �ven f�r hero:...-m�rkning
    public string AltText { get; set; } = "";
}