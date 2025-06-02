using Microsoft.AspNetCore.Components.Forms;
namespace KronoxFront.DTOs;

public class MemberLogoUploadDto
{
    public IBrowserFile File { get; set; } = null!; // IBrowserFile används i Blazor för filuppladdning
    public string AltText { get; set; } = "";
    public int SortOrd { get; set; }
    public string LinkUrl { get; set; } = "";
}