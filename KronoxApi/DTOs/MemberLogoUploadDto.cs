namespace KronoxApi.DTOs;

// För att ladda upp en ny logotyp
public class MemberLogoUploadDto
{
    public IFormFile File { get; set; } = null!;
    public string AltText { get; set; } = "";
    public int SortOrd { get; set; }
    public string LinkUrl { get; set; } = "";
}