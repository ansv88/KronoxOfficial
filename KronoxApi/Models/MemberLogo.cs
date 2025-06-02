namespace KronoxApi.Models;

// Representerar en medlemslogotyp, inklusive bild-URL, alt-text, sorteringsordning och länk.
public class MemberLogo
{
    public int Id { get; set; }
    public string Url { get; set; } = default!;
    public string AltText { get; set; } = default!;
    public int SortOrd { get; set; } // sorteringsordning i karusellen
    public string LinkUrl { get; set; } = ""; // Webbadress (länk) kopplad till logotypen.
}