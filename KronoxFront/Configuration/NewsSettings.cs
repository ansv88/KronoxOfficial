namespace KronoxFront.Configuration;

// Inställningar för nyhetsvisning. Ändras i appsettings.json under "NewsSettings".
public class NewsSettings
{
    public const string SectionName = "NewsSettings";

    // Antal tecken som visas i förhandsvisningen innan texten kortas av med "...".
    public int PreviewLength { get; set; } = 300;
}