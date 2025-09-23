namespace KronoxApi.DTOs;

public class RegisterExistingImageRequest
{
    // Relativ sökväg från wwwroot, t.ex. "images/pages/home/foo.jpg"
    public string SourcePath { get; set; } = "";
    // AltText används även för hero:...-märkning
    public string AltText { get; set; } = "";
}