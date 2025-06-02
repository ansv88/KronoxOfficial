namespace KronoxFront.ViewModels;

public class RegisterPageImageViewModel
{
    public string SourcePath { get; set; } = string.Empty;
    public string PageKey { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public bool PreserveFilename { get; set; } = false;
}