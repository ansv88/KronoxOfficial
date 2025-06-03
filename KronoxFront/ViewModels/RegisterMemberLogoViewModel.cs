namespace KronoxFront.ViewModels;

// ViewModel f�r att registrera en medlemslogotyp
public class RegisterMemberLogoViewModel
{
    public string SourcePath { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string AltText { get; set; } = "";
    public int SortOrd { get; set; }
    public string LinkUrl { get; set; } = "";
}