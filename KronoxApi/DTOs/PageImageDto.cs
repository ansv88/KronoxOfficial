namespace KronoxApi.DTOs;

// DTO för överföring av bildinformation mellan API och klient. Enklare version av PageImage-modellen utan navigationsegenskap.
public class PageImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
}