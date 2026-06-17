namespace KronoxApi.Models;

// Bild kopplad till en sida via PageKey (URL, alt‑text) och länk till innehållsblock.
public class PageImage
{
    public int Id { get; set; }
    public string Url { get; set; } = default!;
    public string AltText { get; set; } = default!;
    public string PageKey { get; set; } = default!; // Detta fält är foreign key till ContentBlock.PageKey
    public bool IsActive { get; set; } = false; // true = bilden är aktivt vald för sin roll på sidan


    // Navigeringsegenskap för att enkelt komma åt innehållsblocket som bilden hör till
    public ContentBlock ContentBlock { get; set; } = default!;
}
