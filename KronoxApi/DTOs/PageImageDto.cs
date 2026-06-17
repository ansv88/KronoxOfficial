namespace KronoxApi.DTOs;

// DTO för överföring av bildinformation mellan API och klient. Enklare version av PageImage-modellen utan navigationsegenskap.
public class PageImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public string PageKey { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class SetImageActiveDto
{
    public bool IsActive { get; set; }
    /// <summary>
    /// Om true inaktiveras övriga bilder med samma PageKey (banner-beteende).
    /// Sätt false för feature/FAQ där flera bilder kan vara aktiva samtidigt.
    /// </summary>
    public bool DeactivateOthers { get; set; } = true;
}
