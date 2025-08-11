namespace KronoxFront.DTOs;

public class MemberLogoDto
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public string AltText { get; set; } = "";
    public int SortOrd { get; set; }
    public string? LinkUrl { get; set; }
}