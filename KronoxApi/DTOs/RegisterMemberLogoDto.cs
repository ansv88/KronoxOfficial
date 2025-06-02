namespace KronoxApi.DTOs;

// För att inkludera bilddata vid behov
public class RegisterMemberLogoDto
{
    public string SourcePath { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string AltText { get; set; } = "";
    public int SortOrd { get; set; }
    public string LinkUrl { get; set; } = "";
}