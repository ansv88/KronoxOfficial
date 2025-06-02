using KronoxApi.Models;

namespace KronoxApi.DTOs;

// DTO för att representera en medlemslogotyp (returneras från API till frontend)
public class MemberLogoDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int SortOrd { get; set; }
    public string LinkUrl { get; set; } = string.Empty;

    // Parameterlös konstruktor för serialisering
    public MemberLogoDto() { }

    // Skapar en DTO från en MemberLogo-entity.
    public MemberLogoDto(MemberLogo l)
    {
        Id = l.Id;
        Url = l.Url;
        AltText = l.AltText;
        SortOrd = l.SortOrd;
        LinkUrl = l.LinkUrl;
    }
}