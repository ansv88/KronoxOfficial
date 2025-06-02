namespace KronoxApi.Models;

// Representerar en bild kopplad till en specifik sida i CMS-systemet
public class PageImage
{
    public int Id { get; set; }
    public string Url { get; set; } = default!;
    public string AltText { get; set; } = default!;
    public string PageKey { get; set; } = default!; // Detta f�lt �r foreign key till ContentBlock.PageKey


    // Navigeringsegenskap f�r att enkelt komma �t inneh�llsblocket som bilden h�r till
    public ContentBlock ContentBlock { get; set; } = default!;
}
